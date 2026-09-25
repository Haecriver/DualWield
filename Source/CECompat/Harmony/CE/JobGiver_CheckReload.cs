using CombatExtended;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using static Unity.Burst.Intrinsics.X86.Avx;

namespace DualWield.CECompat.Harmony
{
    [HarmonyPatch(typeof(JobGiver_CheckReload), "DoReloadCheck")]
    public static class JobGiver_CheckReload_DoReloadCheck
    {
        // replace the first checks on tmpComp + the guns.Add(pawn.equipment.Primary);
        // Start at tmpComp = pawn.equipment?.Primary?.TryGetComp<CompAmmoUser>();
        // ends after the if (tmpComp != null && tmpComp.HasMagazine) statement
        // We find both start and end with the call of pawn.equipment.Primary ...

        // If guns is empty the function will naturally return false anyway
        public static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions,
                MethodBase __originalMethod)
        {
            var getEquipment = AccessTools.Field(
                typeof(Pawn),
                nameof(Pawn.equipment));

            var getPrimary = AccessTools.PropertyGetter(
                typeof(Pawn_EquipmentTracker),
                nameof(Pawn_EquipmentTracker.Primary));

            var add = AccessTools.Method(
                typeof(List<ThingWithComps>),
                nameof(List<ThingWithComps>.Add));

            var listCtor = AccessTools.Constructor(
                typeof(List<ThingWithComps>),
                Type.EmptyTypes
            );     

            // match the guns local
            var matcherGuns = new CodeMatcher(instructions);
            matcherGuns
                .Start()
                .MatchStartForward(
                    new CodeMatch(OpCodes.Newobj, listCtor)
                )
                .Advance(1);
            var gunsStore = matcherGuns.Instruction;

            if (!gunsStore.IsStloc())
            {
                throw new InvalidOperationException("Cannot find guns local");
            }

            // Match the code to replace
            var matcher = new CodeMatcher(instructions);

            matcher
                .MatchStartForward(
                    CodeMatch.LoadsField(getEquipment))
                .ThrowIfInvalid("Reload block start not found");

            // The pawn field is not a simple CodeMatch.IsLdarg(1);
            // it's a ldfld CombatExtended.<>c__DisplayClass3_0::pawn
            // so to use it as a matcher, I save it there.
            var pawnField = (FieldInfo)matcher
                .InstructionAt(-1)
                .operand;

            int start = matcher.Pos - 1;

            matcher
                .MatchEndForward(
                    CodeMatch.LoadsLocal(), // guns
                    CodeMatch.LoadsField(pawnField), // pawn
                    CodeMatch.LoadsField(getEquipment),
                    CodeMatch.Calls(getPrimary),
                    CodeMatch.Calls(add))
                .ThrowIfInvalid("Reload block end not found");

            int end = matcher.Pos;

            var pawnParameter = __originalMethod
                .GetParameters()
                .FirstOrDefault(p => p.ParameterType == typeof(Pawn));

            matcher
                .Start()
                .Advance(start)
                .RemoveInstructionsInRange(start, end)
                .Insert(
                    // There are two labels to pop before inserting the replacement function
                    new CodeInstruction(OpCodes.Pop),
                    new CodeInstruction(OpCodes.Pop),
                    CodeInstruction.LoadArgument(pawnParameter.Position + 1),
                    CodeInstruction.LoadLocal(gunsStore.LocalIndex()),
                    CodeInstruction.Call(
                        typeof(JobGiver_CheckReload_DoReloadCheck),
                        nameof(Replacement)));

            return matcher.InstructionEnumeration();
        }

        public static void Replacement(Pawn pawn, List<ThingWithComps> guns)
        {
            ThingWithComps primaryEquip = pawn.equipment?.Primary;
            CompAmmoUser primaryComp = primaryEquip?.TryGetComp<CompAmmoUser>();

            ThingWithComps offHandEquip = null;
            CompAmmoUser offHandComp = null;

            // Check if there are two weapons
            if (!(primaryEquip?.IsOffHand() ?? false) && pawn.equipment.TryGetOffHandEquipment(out offHandEquip))
            {
                offHandComp = offHandEquip?.TryGetComp<CompAmmoUser>();
            }

            if (pawn.Drafted)
            {
                // nothing can be done if primary is null
                if (primaryComp == null)
                {
                    return;
                }

                // Test for primary
                if (!CheckCompAmmoUser(primaryComp, pawn))
                {
                    // set it to null to cancel it
                    primaryComp = null;
                }

                // Test of offHand
                if (!CheckCompAmmoUser(offHandComp, pawn))
                {
                    // set it to null to cancel it
                    offHandComp = null;
                }
            }

            // Add guns to the next potential jobs
            if (primaryComp != null && primaryComp.HasMagazine)
            {
                guns.Add(primaryEquip);
            }

            if (offHandComp != null && offHandComp.HasMagazine)
            {
                guns.Add(offHandEquip);
            }
        }

        // Return true if the comp can be added, else return false
        // This is more or less a little part of the CE code
        public static bool CheckCompAmmoUser(CompAmmoUser cmp, Pawn pawn)
        {
            if (cmp == null)
            {
                return false;
            }
            if (!cmp.IsOpportunisticReloadActive)
            {
                return false;
            }
            if (Find.TickManager.TicksGame - pawn.LastAttackTargetTick < cmp.MinimalTicksAfterFight)
            {
                return false;
            }
            var enemiesAround = pawn.Map.mapPawns.AllPawnsSpawned.Where(x => x.Position.InHorDistOf(pawn.Position, cmp.SafeDistanceToReload) && !x.IsPsychologicallyInvisible() && x.HostileTo(pawn));
            if (enemiesAround.Any())
            {
                return false;
            }
            return true;
        }
    }
}
