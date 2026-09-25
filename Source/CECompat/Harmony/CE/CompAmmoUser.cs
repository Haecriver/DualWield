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
        static void Postfix(CompAmmoUser __instance, int ____lastReloadJobTick) // damned field is named _lastReloadJobTick 😵
        {
            // This weapon needs to be reload anyway
            if (__instance.IsEquippedGun && ____lastReloadJobTick != GenTicks.TicksGame && CompAmmoUser_PatchUtils.DoByPassWeapon(__instance.parent))
            {
                // Here we know we need to queue a reload job for this comp
                Job reloadJob = __instance.TryMakeReloadJob();
                if (reloadJob == null)
                {
                    return;
                }
                ____lastReloadJobTick = GenTicks.TicksGame;
                reloadJob.playerForced = true;
                __instance.Wielder.jobs.StartJob(reloadJob, JobCondition.Succeeded, null, true);
            }
        }
    }
}
