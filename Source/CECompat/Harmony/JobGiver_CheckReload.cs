using CombatExtended;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace DualWield.CECompat.Harmony
{
    [HarmonyPatch(typeof(JobGiver_CheckReload), "DoReloadCheck")]
    public static class JobGiver_CheckReload_DoReloadCheck
    {
        public static IEnumerable<CodeInstruction> Transpiler(
            ILGenerator il,
            IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions, il);

            var changeTmpComp = AccessTools.Method(
                typeof(JobGiver_CheckReload_DoReloadCheck),
                nameof(ChangeTmpComp));

            var changeGun = AccessTools.Method(
                typeof(JobGiver_CheckReload_DoReloadCheck),
                nameof(ChangeGun));


            // ------------------------------------------------------------------
            // tmpComp = pawn.equipment?.Primary?.TryGetComp<CompAmmoUser>();
            // ------------------------------------------------------------------
            
            matcher.MatchStartForward(
                new CodeMatch(ci =>
                {
                    if (ci.opcode != OpCodes.Call || !(ci.operand is MethodInfo method))
                    {
                        return false;
                    }

                    return method.DeclaringType == typeof(ThingCompUtility)
                        && method.Name == nameof(ThingCompUtility.TryGetComp)
                        && method.IsGenericMethod
                        && method.GetGenericArguments()[0] == typeof(CompAmmoUser);
                })
            );

            if (matcher.IsInvalid)
            {
                throw new Exception("Could not find tmpComp TryGetComp<CompAmmoUser>() call.");
            }

            matcher.Advance(1);

            matcher.MatchStartForward(
                new CodeMatch(ci =>
                    ci.opcode == OpCodes.Stloc ||
                    ci.opcode == OpCodes.Stloc_S ||
                    ci.opcode == OpCodes.Stloc_0 ||
                    ci.opcode == OpCodes.Stloc_1 ||
                    ci.opcode == OpCodes.Stloc_2 ||
                    ci.opcode == OpCodes.Stloc_3)
            );

            if (matcher.IsInvalid) 
            {
                throw new Exception("Could not find tmpComp local store.");
            }

            var tmpCompLocal = il.DeclareLocal(typeof(CompAmmoUser));

            matcher.Insert(
                new CodeInstruction(OpCodes.Stloc, tmpCompLocal),
                new CodeInstruction(OpCodes.Ldarg_1), // pawn
                new CodeInstruction(OpCodes.Ldloc, tmpCompLocal),
                new CodeInstruction(OpCodes.Call, changeTmpComp)
            );

            // ------------------------------------------------------------------
            // guns.Add(pawn.equipment.Primary);
            // ------------------------------------------------------------------

            var addMethod = AccessTools.Method(
               typeof(List<ThingWithComps>),
               nameof(List<ThingWithComps>.Add),
               new[] { typeof(ThingWithComps) }
            );

            matcher.MatchStartForward(
                new CodeMatch(OpCodes.Callvirt, addMethod)
            );

            if (matcher.IsInvalid)
            {
                throw new Exception("Could not find guns.Add(pawn.equipment.Primary).");
            }

            var gunLocal = il.DeclareLocal(typeof(ThingWithComps));

            // Stack before Add:
            // [guns, item]
            //
            // Keep guns on the stack while changing item.
            matcher.Insert(
                new CodeInstruction(OpCodes.Stloc, gunLocal),
                new CodeInstruction(OpCodes.Ldarg_1), // pawn
                new CodeInstruction(OpCodes.Ldloc, gunLocal),
                new CodeInstruction(OpCodes.Call, changeGun)
            );

            return matcher.Instructions();
        }

        public static ThingWithComps ChangeGun(Pawn pawn, ThingWithComps original)
        {
            if (original != null)
            {
                return original;
            }

            if (pawn.equipment.TryGetOffHandEquipment(out ThingWithComps offHandEquip))
            {
                return offHandEquip;
            }
            return null;
        }

        public static CompAmmoUser ChangeTmpComp(Pawn pawn, CompAmmoUser original)
        {
            if (original != null)
            {
                return original;
            }

            if (pawn.equipment.TryGetOffHandEquipment(out ThingWithComps offHandEquip))
            {
                return offHandEquip?.TryGetComp<CompAmmoUser>();
            }
            return null;
        }
    }
}
