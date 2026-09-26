using CombatExtended;
using DualWield.CECompat.Gizmos;
using HarmonyLib;
using Verse;

namespace DualWield.CECompat.Harmony
{
    [HarmonyPatch(typeof(Command_Reload), nameof(Command_Reload.GroupsWith))]
    public static class Command_Reload_GroupsWith
    {
        public static void Postfix(Gizmo other, ref bool __result)
        {
            __result &= !(other is Command_ReloadOffhand);
        }
    }

    [HarmonyPatch(typeof(Command_Reload), nameof(Command_Reload.MergeWith))]
    public static class Command_Reload_MergeWith
    {
        public static bool Prefix(Gizmo other)
        {
            if (other is Command_ReloadOffhand)
            {
                return false;
            }
            return true;
        }
    }
}
