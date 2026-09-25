using CombatExtended;
using CombatExtended.AI;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;
using Verse.AI;

namespace DualWield.CECompat.Harmony
{
    // Look for any call for pawn.equipment.Primary in JobDriver_Reload

    // This Getter is comparing weapon to pawn.equipement.Primary
    // We add an OR condition that also check if weapon is offHand equipment
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


    // This Getter check if Primary is not the used weapon. It is a FailOn condition for the reload job.
    // And if there is a compReloader.
    // So if this function returns true, it means either, Primary Has NoGunOrAmmo, or compReloader should not be reload
    // So in that case, we need to check if compReloader is fine, then we can return true if offHand Equip has NoGunOrAmmo
    // If offHand Equip is a gun with ammo, in that case, we should returns false, because we want the job to continue
    [HarmonyPatch(typeof(JobDriver_Reload), "HasNoGunOrAmmo")]
    public static class JobDriver_Reload_HasNoGunOrAmmo
    {
        public static void Postfix(
            JobDriver_Reload __instance,
            ref bool __result,
            ref bool ___reloadingEquipment,
            ref ThingWithComps ___initEquipment
        )
        {
            if (!__result)
            {
                // no need to do anything then, the job will continue
                return;
            }

            var compReloader = AccessTools.Property(typeof(JobDriver_Reload), "compReloader")
                .GetValue(__instance) as CompAmmoUser;

            if (compReloader == null || !compReloader.HasAndUsesAmmoOrMagazine)
            {
                // the job needs to stop anyway
                return;
            }

            var pawn = __instance.pawn;
            var weapon = AccessTools.Property(typeof(JobDriver_Reload), "weapon")
                .GetValue(__instance) as ThingWithComps;

            // There was no need for primary, is the is no need for offHand, the job can stop
            __result &=
                // has offhand
                pawn?.equipment != null && pawn.equipment.TryGetOffHandEquipment(out ThingWithComps offHandEquip)

                // Rewrite the logic but for the offhand equipment (we don't need the inventory check as it was already covered)
                && ((___reloadingEquipment && (offHandEquip == null || offHandEquip != weapon))
                 || (___initEquipment != offHandEquip));
        }
    }

    // inject secondary equipment in MakeNewToils
    // We need to match and fill the line 157
    // initEquipment = pawn.equipment?.Primary;
    // Because initEquipment will be later used.
    // The test we need to do is to check if the weapon linked to the job is Primary or OffHand weapon.
    // If it is one of these, we need to return the correct equipment to initEquipment
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
                // And duplicate it for our function
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
