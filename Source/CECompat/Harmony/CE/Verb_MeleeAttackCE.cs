using CombatExtended;
using DualWield.Harmony;
using HarmonyLib;
using System.Collections.Generic;

namespace DualWield.CECompat.Harmony
{
    // The same logic is applicable for CE
    [HarmonyPatch(typeof(Verb_MeleeAttackCE), "TryCastShot")]
    public class Verb_MeleeAttackCE_TryCastShot
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return Verb_MeleeAttack_TryCastShot.Transpiler(instructions);
        }
    }
}
