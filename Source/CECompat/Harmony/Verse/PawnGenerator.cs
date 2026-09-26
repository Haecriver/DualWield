using CombatExtended;
using HarmonyLib;
using Verse;

namespace DualWield.CECompat.Harmony
{
    [HarmonyPatch(typeof(PawnGenerator), "GenerateGearFor")]
    public static class PawnGenerator_GenerateGearFor
    {
        public static void Postfix(Pawn pawn)
        {
            // no shield with off hand weapons
            if (pawn.equipment.TryGetOffHandEquipment(out ThingWithComps offHandEquip))
            {
                if (pawn.apparel != null)
                {
                    var list = pawn.apparel.WornApparel;
                    // Reverse loop to prevent removal issues
                    for (int i = list.Count - 1; i >= 0; i--)
                    {
                        if (list[i] is Apparel_Shield shield)
                        {
                            if (!shield.Destroyed)
                            {
                                shield.Destroy();
                            }
                        }
                    }
                }
            }
        }
    }
}