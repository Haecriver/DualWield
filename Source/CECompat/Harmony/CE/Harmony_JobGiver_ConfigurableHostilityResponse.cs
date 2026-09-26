using CombatExtended;
using CombatExtended.AI;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace DualWield.CECompat.Harmony
{
    [HarmonyPatch(typeof(JobGiver_ConfigurableHostilityResponse), "TryGetAttackNearbyEnemyJob")]
    internal class Harmony_JobGiver_ConfigurableHostilityResponse
    {
        internal static void Postfix(Pawn pawn, ref Job __result)
        {
            if (__result != null && __result.def == JobDefOf.AttackStatic)
            {
                if (!pawn.equipment.Primary.IsOffHand() && pawn.equipment.TryGetOffHandEquipment(out ThingWithComps offHandEquip))
                {
                    // Check for reload
                    var ammoComp = offHandEquip.TryGetComp<CompAmmoUser>();
                    if (ammoComp == null)
                    {
                        return;
                    }

                    if (!ammoComp.CanBeFiredNow)
                    {
                        __result = ammoComp.HasAmmo && !(pawn.jobs?.curDriver is IJobDriver_Tactical) ?
                                   JobMaker.MakeJob(CE_JobDefOf.ReloadWeapon, pawn, offHandEquip) :
                                   JobMaker.MakeJob(JobDefOf.AttackMelee, __result.targetA);
                    }
                }
            }
        }
    }
}
