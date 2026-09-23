using Verse;

namespace DualWield.CECompat
{
    public class CECompat : Mod
    {
        public CECompat(ModContentPack content) : base(content)
        {
            var harmony = new HarmonyLib.Harmony("DualWield.CECompat");
            harmony.PatchAll();
            Log.Message("Combat Extended - Dual Wield compatibility patch is loaded ! 🔫🔫");
        }
    }
}
