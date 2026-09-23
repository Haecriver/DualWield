using CombatExtended;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;

namespace DualWield.CECompat.Harmony
{
    [HarmonyPatch(typeof(JobGiver_CheckReload), "DoReloadCheck")]
    public static class JobGiver_CheckReload_DoReloadCheck
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            var addMethod = AccessTools.Method(
                typeof(List<ThingWithComps>),
                nameof(List<ThingWithComps>.Add));

            var replacementMethod = AccessTools.Method(
                typeof(JobGiver_CheckReload_DoReloadCheck),
                nameof(AddPrimaryAndExtraGun));

            for (int i = 0; i < codes.Count; i++)
            {
                if (!codes[i].Calls(addMethod))
                    continue;

                // The stack contains:
                // [guns, primary]
                //
                // Add pawn so the helper receives:
                // [guns, primary, pawn]
                codes[i] = new CodeInstruction(OpCodes.Ldarg_1);

                codes.Insert(
                    i + 1,
                    new CodeInstruction(OpCodes.Call, replacementMethod));

                return codes;
            }

            throw new System.Exception("Could not find List<ThingWithComps>.Add in JobGiver_CheckReload.DoReloadCheck.");
        }

        private static void AddPrimaryAndExtraGun(
            List<ThingWithComps> guns,
            ThingWithComps primary,
            Pawn pawn)
        {
            // Preserve the original behavior.
            guns.Add(primary);

            if (pawn.equipment.TryGetOffHandEquipment(out ThingWithComps offHandEquip))
            {
                guns.Add(offHandEquip);
            }
        }
    }
}
