using CombatExtended;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;

namespace DualWield.CECompat.Harmony
{
    [HarmonyPatch(typeof(JobDriver_Reload), "weaponEquipped", MethodType.Getter)]
    public static class JobDriver_Reload_weaponEquipped
    {
        public static void Postfix(JobDriver_Reload __instance, ref bool __result)
        {
            var pawn = AccessTools.Field(typeof(JobDriver_Reload), "pawn")
                .GetValue(__instance) as Pawn;

            var weapon = AccessTools.Property(typeof(JobDriver_Reload), "weapon")
                .GetValue(__instance) as ThingWithComps;

            __result |= 
                // has offhand
                pawn != null 
                && pawn.equipment.TryGetOffHandEquipment(out ThingWithComps offHandEquip)
                // offhand is the targeted weapon
                && offHandEquip == weapon;
        }
    }

    [HarmonyPatch(typeof(JobDriver_Reload), "HasNoGunOrAmmo")]
    public static class JobDriver_Reload_HasNoGunOrAmmo
    {
        public static void Postfix(JobDriver_Reload __instance, ref bool __result)
        {
            var pawn = AccessTools.Field(typeof(JobDriver_Reload), "pawn")
                .GetValue(__instance) as Pawn;

            var reloadingEquipment = AccessTools.Field(typeof(JobDriver_Reload), "reloadingEquipment")
                .GetValue(__instance) as bool?;

            var weapon = AccessTools.Property(typeof(JobDriver_Reload), "weapon")
                .GetValue(__instance) as ThingWithComps;

            var initEquipment = AccessTools.Field(typeof(JobDriver_Reload), "initEquipment")
                .GetValue(__instance) as ThingWithComps;

            // fail if true
            __result &=
                // has offhand
                (pawn != null
                && reloadingEquipment.HasValue
                && pawn.equipment.TryGetOffHandEquipment(out ThingWithComps offHandEquip))

                // Rewrite the logic but for the offhand equipment
                && ((reloadingEquipment.Value && (offHandEquip == null || offHandEquip != weapon))
                 || (initEquipment != offHandEquip));
        }
    }

    // inject secondary equipment in MakeNewToils
    [HarmonyPatch(typeof(JobDriver_Reload), nameof(JobDriver_Reload.MakeNewToils), MethodType.Enumerator)]
    public static class JobDriver_Reload_MakeNewToils_Patch
    {
        public static IEnumerable<CodeInstruction> Transpiler(
            ILGenerator il,
            IEnumerable<CodeInstruction> instructions)
        {
            var initEquipment = AccessTools.Field(
                typeof(JobDriver_Reload),
                "initEquipment");

            var changeValue = AccessTools.Method(
                typeof(JobDriver_Reload_MakeNewToils_Patch),
                nameof(ChangeInitEquipment));

            var localDriver = il.DeclareLocal(typeof(JobDriver_Reload));
            var localValue = il.DeclareLocal(typeof(ThingWithComps));

            var matcher = new CodeMatcher(instructions, il);

            matcher.MatchStartForward(
                new CodeMatch(OpCodes.Stfld, initEquipment)
            );

            if (matcher.IsInvalid)
            {
                throw new Exception("initEquipment assignment not found.");
            }

            matcher.Insert(
                new CodeInstruction(OpCodes.Stloc, localValue),
                new CodeInstruction(OpCodes.Stloc, localDriver),

                // Reintroduce an instance of JobDriver_Reload
                new CodeInstruction(OpCodes.Ldloc, localDriver),
                // And dupicate it for our function
                new CodeInstruction(OpCodes.Dup),

                // Reintroduce original ThingWithComps
                new CodeInstruction(OpCodes.Ldloc, localValue),

                // (JobDriver_Reload, ThingWithComps) -> ThingWithComps
                new CodeInstruction(OpCodes.Call, changeValue)
            );

            return matcher.Instructions();
        }

        private static ThingWithComps ChangeInitEquipment(
            JobDriver_Reload instance,
            ThingWithComps original)
        {
            var reloadingEquipment = AccessTools.Field(typeof(JobDriver_Reload), "reloadingEquipment").GetValue(instance) as bool?;
            var weapon = AccessTools.Property(typeof(JobDriver_Reload), "weapon").GetValue(instance) as ThingWithComps;

            // Return original if
            // - not reloading equipement
            // - no Primary fetched
            // - we're trying to reload main hand
            if ((reloadingEquipment.HasValue && !reloadingEquipment.Value) || original == null || weapon == original)
            {
                return original;
            }

            // Try to get OffHandEquipment          
            if (instance.pawn.equipment.TryGetOffHandEquipment(out ThingWithComps offHandEquip))
            {
                // We're trying to reload the offhand weapon
                if (weapon == offHandEquip)
                {
                    return offHandEquip;
                }
            }

            Log.Warning($"Tried to reload {weapon}, but it was neither in main hand nor in off hand");
            return null;
        }
    }
}
