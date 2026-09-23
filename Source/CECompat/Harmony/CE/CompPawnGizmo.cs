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
        public static void Postfix(
            bool ___duplicate,
            ThingWithComps ___parent,
            CompPawnGizmo __instance,
            ref IEnumerable<Gizmo> __result
        )
        {
            if (!___duplicate)
            {
                var pawn = ___parent as Pawn;
                var equip = pawn != null
                         ? pawn.equipment
                         : null;
                var primary = equip?.Primary;

                if (equip != null && equip.TryGetOffHandEquipment(out ThingWithComps offHandEquip))
                {
                    if ((offHandEquip != null) && (!offHandEquip.AllComps.NullOrEmpty()))
                    {
                        __result = GetGizmosWithOffhand(__result, offHandEquip.AllComps, primary);
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
