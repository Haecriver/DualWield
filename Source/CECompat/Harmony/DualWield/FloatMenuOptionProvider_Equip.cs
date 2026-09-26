using CombatExtended;
using HarmonyLib;
using RimWorld;
using Verse;

namespace DualWield.CECompat.Harmony
{
    [HarmonyPatch(typeof(FloatMenuOptionProvider_Equip), "GetSingleOptionFor")]
    public static class FloatMenuOptionProvider_Equip_GetSingleOptionFor
    {
        public static void Postfix(Thing clickedThing, FloatMenuContext context, ref FloatMenuOption __result)
        {
            if (CE_Utility.HasShield(context.FirstSelectedPawn))
            {
                __result = new FloatMenuOption("CannotEquip".Translate(clickedThing.LabelShort) + " " + "DW_AsOffHand".Translate() + " (" + "DW_OffHandWithShield".Translate() + ")", null);
            }
        }
    }
}