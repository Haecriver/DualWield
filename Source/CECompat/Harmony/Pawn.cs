using CombatExtended;
using DualWield.CECompat.Gizmos;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace DualWield.CECompat.Harmony
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos), MethodType.Enumerator)]
    public static class Pawn_GetGizmos
    {
        public static IEnumerable<CodeInstruction> Transpiler(
            MethodBase __originalMethod,
            ILGenerator il,
            IEnumerable<CodeInstruction> instructions)
        {
            var equipment = AccessTools.Field(
            typeof(Pawn),
            nameof(Pawn.equipment));

            var getGizmos = AccessTools.Method(
                typeof(Pawn_EquipmentTracker),
                nameof(Pawn_EquipmentTracker.GetGizmos));

            var getGizmosWithOffhand = AccessTools.Method(
                typeof(Pawn_GetGizmos),
                nameof(GetGizmosWithOffhand));

            var matcher = new CodeMatcher(instructions, il);

            matcher.MatchStartForward(
                new CodeMatch(OpCodes.Ldfld, equipment),
                new CodeMatch(OpCodes.Callvirt, getGizmos)
            );

            if (matcher.IsInvalid)
            {
                Log.Error("Pawn.GetGizmos: equipment.GetGizmos() not found.");
                return instructions;
            }

            matcher.Advance(1);
            matcher.SetInstruction(
                new CodeInstruction(OpCodes.Call, getGizmosWithOffhand)
            );

            return matcher.Instructions();
        }

        public static IEnumerable<Gizmo> GetGizmosWithOffhand(Pawn_EquipmentTracker equipment)
        {
            foreach (Gizmo gizmo in equipment.GetGizmos())
            {
                // if equipement does not have main hand, do not draw GizmoAmmoSatus
                if (equipment.Primary != null && !equipment.Primary.IsOffHand())
                {
                    if (!(gizmo is GizmoAmmoStatus))
                    {
                        yield return gizmo;
                    }
                } 
                else
                {
                    yield return gizmo;
                }
            }

            if (equipment.TryGetOffHandEquipment(out ThingWithComps offHandEquip))
            {
                foreach (Gizmo gizmo in offHandEquip.GetGizmos())
                {
                    if (gizmo is GizmoAmmoStatus gizmoAmmoStatus)
                    {
                        yield return new GizmoAmmoStatusOffsethand(gizmoAmmoStatus);
                    } 
                    else
                    {
                        yield return gizmo;
                    }
                }
            }
        }
    }
}
