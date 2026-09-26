using HarmonyLib;
using RimWorld.BaseGen;
using Verse;

namespace DualWield.CECompat.Harmony
{
    [HarmonyPatch(typeof(Ext_ThingDef), nameof(Ext_ThingDef.CanBeOffHand))]
    public static class Ext_ThingDef_CanBeOffHand
    {
        public static bool Prefix(ThingDef td, bool __result)
        {
            if (!DualWield.Settings.UseCombatExentedConfig)
            {
                return true;
            }
            __result = td != null && td.weaponTags.Contains("CE_OneHandedWeapon");
            return false;
        }
    }

    [HarmonyPatch(typeof(Ext_ThingDef), nameof(Ext_ThingDef.IsTwoHand))]
    public static class Ext_ThingDef_IsTwoHand
    {
        public static bool Prefix(ThingDef td, bool __result)
        {
            if (!DualWield.Settings.UseCombatExentedConfig)
            {
                return true;
            }
            __result = td != null && !td.weaponTags.Contains("CE_OneHandedWeapon");
            return false;
        }
    }
}