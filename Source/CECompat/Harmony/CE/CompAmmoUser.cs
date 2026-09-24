using CombatExtended;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using Verse.AI;

// I tried to queue a reload job for secondary weapon on gizmo click, but it did not work easily
// As the other gun is always reloaded in the end I guess it's ok

namespace DualWield.CECompat.Harmony
{
    public static class CompAmmoUser_PatchUtils
    {
        public static List<int> __weaponToByPass = new List<int>();

        public static void RegisterWeapon(ThingWithComps thing)
        {
            __weaponToByPass.Add(thing.thingIDNumber);
        }

        public static bool DoByPassWeapon(ThingWithComps thing)
        {
            int index = __weaponToByPass.FindIndex((id) => id == thing.thingIDNumber);
            if (index != -1)
            {
                __weaponToByPass.RemoveAt(index);
                return true;
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(CompAmmoUser), "SyncedTryStartReload")]
    public static class CompAmmoUser_CompGetGizmosExtra
    {
        // This function is only called on Command action (which is perfect for us)
        public static void Postfix(CompAmmoUser __instance)
        {
            bool primaryIsMainHand = !__instance.parent.IsOffHand();
            // we need to call TryStartReload on offHand gun if necessary
            if (primaryIsMainHand && __instance.Wielder.equipment.TryGetOffHandEquipment(out ThingWithComps offHandEquip))
            {
                // then there is a second weapon
                CompAmmoUser_PatchUtils.RegisterWeapon(offHandEquip);
                offHandEquip.TryGetComp<CompAmmoUser>()?.TryStartReload();
            }
        }
    }

    [HarmonyPatch(typeof(CompAmmoUser), nameof(CompAmmoUser.TryStartReload))]
    public static class CompAmmoUser_TryStartReload
    {
        // Try to intercept the  (Wielder.jobs.curJob?.def ?? null) != CE_JobDefOf.ReloadWeapon condition only when needed
        // (which means when the weapon is in __weaponToByPass) 
        static IEnumerable<CodeInstruction> Transpiler(
                  IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            FieldInfo reloadWeaponField =
                AccessTools.Field(
                    typeof(CE_JobDefOf),
                    nameof(CE_JobDefOf.ReloadWeapon)
                );

            MethodInfo getReloadDefMethod =
                AccessTools.Method(
                    typeof(CompAmmoUser_TryStartReload),
                    nameof(GetReloadDefForComparison)
                );

            matcher
                .MatchStartForward(
                    new CodeMatch(OpCodes.Ldsfld, reloadWeaponField)
                )
                .ThrowIfInvalid(
                    "Cannot find CE_JobDefOf.ReloadWeapon in CompAmmoUser.TryStartReload"
                );

            // Preserve any label attached to the original instruction.
            matcher.Set(OpCodes.Ldarg_0, null);
            matcher.Advance(1);

            matcher.Insert(
                new CodeInstruction(
                    OpCodes.Call,
                    getReloadDefMethod
                )
            );

            return matcher.InstructionEnumeration();
        }

        private static JobDef GetReloadDefForComparison(CompAmmoUser comp)
        {
            if (CompAmmoUser_PatchUtils.DoByPassWeapon(comp.parent))
            {
                // Here we know we need to queue a reload job for this comp
                Job reloadJob = comp.TryMakeReloadJob();
                if (reloadJob != null)
                {
                    reloadJob.playerForced = true;
                    comp.Wielder.jobs.StartJob(reloadJob, JobCondition.Succeeded, null, true);
                }
            }
            return CE_JobDefOf.ReloadWeapon;
        }
    }
}
