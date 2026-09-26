using CombatExtended;
using DualWield.CECompat.Gizmos;
using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
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
            foreach (var gizmo in original)
            {
                // Don't draw primary if offHand (because it means there is only off hand weapon, and we don't want to draw its gizmo)
                if (
                    !primary.IsOffHand() 
                    || (!gizmo.IsGizmoAmmoStatus(out _) 
                        && !gizmo.IsCommandReload(out _)
                        && !gizmo.IsFireModeToggle(out _)
                        && !gizmo.IsAimModeToggle(out _)))
                yield return gizmo;
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
                        if (gizmo.IsGizmoAmmoStatus(out GizmoAmmoStatus gizmoAmmoStatus))
                        {
                            yield return new GizmoAmmoStatusOffhand(gizmoAmmoStatus);
                        }
                        else if (gizmo.IsCommandReload(out Command_Reload command_Reload))
                        {
                            yield return new Command_ReloadOffhand()
                            {
                                compAmmo = command_Reload.compAmmo,
                                action = command_Reload.action,
                                defaultLabel = $"(2) {command_Reload.defaultLabel}",
                                defaultDesc = command_Reload.defaultDesc,
                                icon = command_Reload.icon,
                                tutorTag = command_Reload.tutorTag
                            };
                        }
                        else 
                        {
                            Command_Action command_Action;
                            if (gizmo.IsAimModeToggle(out command_Action) || gizmo.IsFireModeToggle(out command_Action))
                            {
                                yield return new Command_Action()
                                {
                                    action = command_Action.action,
                                    defaultLabel = $"(2) {command_Action.defaultLabel}",
                                    defaultDesc = command_Action.defaultDesc,
                                    icon = command_Action.icon,
                                    tutorTag = command_Action.tutorTag
                                };
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
        private static bool IsGizmoAmmoStatus(this Gizmo gizmo, out GizmoAmmoStatus gizmoAmmoStatus) => (gizmoAmmoStatus = gizmo as GizmoAmmoStatus) != null;
        private static bool IsCommandReload(this Gizmo gizmo, out Command_Reload command_Reload) => (command_Reload = gizmo as Command_Reload) != null;
        private static bool IsFireModeToggle(this Gizmo gizmo, out Command_Action caFireModeToogle) => (caFireModeToogle = gizmo as Command_Action) != null 
            && caFireModeToogle.defaultDesc == "CE_ToggleFireModeDesc".Translate();
        private static bool IsAimModeToggle(this Gizmo gizmo, out Command_Action caAimModeToogle) => (caAimModeToogle = gizmo as Command_Action) != null
            && caAimModeToogle.defaultDesc == "CE_ToggleAimModeDesc".Translate();
    }
}
