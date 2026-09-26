using CombatExtended;
using HarmonyLib;
using RimWorld;
using Verse;

namespace DualWield.Harmony.CE
{
    [HarmonyPatch(typeof(Verb_LaunchProjectileCE), nameof(Verb_LaunchProjectileCE.SightsEfficiency), MethodType.Getter)]
    public static class Verb_LaunchProjectileCE_SwayAmplitude
    {
        public static void Postfix(Verb_LaunchProjectileCE __instance, float __result)
        {
            Thing equipment = __instance.EquipmentSource;
            if (!(equipment is { ParentHolder: Pawn_EquipmentTracker peqt })) return;

            var pawn = peqt.pawn;

            if (!pawn.equipment.TryGetOffHandEquipment(out _))
                return;

            var skillLevel = pawn.skills == null
                ? 8
                : pawn.skills.GetSkill(SkillDefOf.Shooting).levelInt;

            var staticPenalty = (equipment is ThingWithComps twc && twc.IsOffHand()
                ? DualWield.Settings.StaticAccPOffHand
                : DualWield.Settings.StaticAccPMainHand) / 100f;
            var dynamicPenalty = (DualWield.Settings.DynamicAccP / 100f) * (20 - skillLevel);

            // Sway factor are better when closer to 0
            // So we augment it
            __result *= 1.0f + staticPenalty + dynamicPenalty;
        }
    }
}
