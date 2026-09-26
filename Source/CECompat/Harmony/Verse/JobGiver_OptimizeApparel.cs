using CombatExtended;
using HarmonyLib;
using RimWorld;
using Verse;

namespace DualWield.CECompat.Harmony
{
    ///Give melee/ballistic shields a negative apparel score if the pawn has off hand weapon.
    ///Mimics vanilla behavior regarding shield belts and ranged weapons (and ce two-handed with shield)
    [HarmonyPatch(typeof(JobGiver_OptimizeApparel), nameof(JobGiver_OptimizeApparel.ApparelScoreGain))]
    public static class JobGiver_OptimizeApparel_ApparelScoreGain
    {
        public static bool Prefix(Pawn pawn, Apparel ap, ref float __result)
        {
            var hasOffHandWeapon = pawn.equipment.TryGetOffHandEquipment(out _);
            if (ap is Apparel_Shield && !hasOffHandWeapon)
            {
                __result = -1000f;
                return false;
            }
            return true;
        }
    }
}