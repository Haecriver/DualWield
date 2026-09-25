using CombatExtended;
using DualWield.CECompat.Gizmos;
using HarmonyLib;
using System.Collections.Generic;
using Verse;

namespace DualWield.CECompat.Harmony
{
    [HarmonyPatch(typeof(CompPawnGizmo), nameof(CompPawnGizmo.CompGetGizmosExtra))]
    public static class CompPawnGizmo_CompGetGizmosExtra
    {
        // Add Gizmo to the result if needed
        public static void Postfix(
            bool ___duplicate,
            ThingWithComps ___parent,
            CompPawnGizmo __instance,
            ref IEnumerable<Gizmo> __result
        )
        {
            // add offHand gizmo if needed
            if (!___duplicate)
            {
                var pawn = ___parent as Pawn;
                if (pawn?.equipment != null && pawn.equipment.TryGetOffHandEquipment(out ThingWithComps offHandEquip))
                {
                    if ((offHandEquip != null) && (!offHandEquip.AllComps.NullOrEmpty()))
                    {
                        __result = GetGizmosWithOffhand(__result, offHandEquip.AllComps, pawn.equipment.Primary);
                    }
                }
            }
        }

        public static IEnumerable<Gizmo> GetGizmosWithOffhand(IEnumerable<Gizmo> original, List<ThingComp> AllComps, ThingWithComps primary)
        {
            if (!primary.IsOffHand())
            {
                foreach (var gizmo in original)
                {
                    yield return gizmo;
                }
            }
            foreach (var comp in AllComps)
            {
                var gizmoGiver = comp as CompRangedGizmoGiver;
                if (
                    (gizmoGiver != null) &&
                    (gizmoGiver.isRangedGiver)
                )
                {
                    foreach (var gizmo in gizmoGiver.CompGetGizmosExtra())
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
}
