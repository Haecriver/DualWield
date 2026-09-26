using CombatExtended;
using HarmonyLib;
using RimWorld;
using Verse;

namespace DualWield.CECompat.Harmony
{
    [HarmonyPatch(typeof(Pawn_ApparelTracker), nameof(Pawn_ApparelTracker.Wear))]
    public static class Pawn_ApparelTracker_Wear
    {
        public static void Prefix(Pawn_ApparelTracker __instance, Apparel newApparel, bool dropReplacedApparel, bool locked)
        {
            Pawn p = __instance.pawn;
            if (newApparel is Apparel_Shield && p.equipment.TryGetOffHandEquipment(out ThingWithComps offHandEquip))
            {
                p.equipment.TryTransferEquipmentToContainer(offHandEquip, p.inventory.innerContainer);
            }
        }
    }
}
