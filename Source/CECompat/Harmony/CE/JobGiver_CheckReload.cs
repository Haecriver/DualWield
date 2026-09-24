using CombatExtended;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Verse;

namespace DualWield.CECompat.Harmony
{
    [HarmonyPatch(typeof(JobGiver_CheckReload), "DoReloadCheck")]
    public static class JobGiver_CheckReload_DoReloadCheck
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
           
            // --------------------------------------------------------------------
            // !tmpComp.IsOpportunisticReloadActive
            // --------------------------------------------------------------------

            var opportunisticReloadGetter = AccessTools.PropertyGetter(
                typeof(CompAmmoUser),
                nameof(CompAmmoUser.IsOpportunisticReloadActive));

            var opportunisticReloadReplacement = AccessTools.Method(
                typeof(JobGiver_CheckReload_DoReloadCheck),
                nameof(ModifyIsOpportunisticReloadActive),
                new[]
                {
            typeof(bool),
            typeof(Pawn)
                });

            // --------------------------------------------------------------------
            // tmpComp.HasMagazine
            // --------------------------------------------------------------------

            var hasMagazineGetter = AccessTools.PropertyGetter(
                typeof(CompAmmoUser),
                nameof(CompAmmoUser.HasMagazine));

            var hasMagazineReplacement = AccessTools.Method(
                typeof(JobGiver_CheckReload_DoReloadCheck),
                nameof(ModifyHasMagazine),
                new[]
                {
            typeof(Pawn)
                });

            // --------------------------------------------------------------------
            // guns.Add(pawn.equipment.Primary)
            // --------------------------------------------------------------------

            var addMethod = AccessTools.Method(
                typeof(List<ThingWithComps>),
                nameof(List<ThingWithComps>.Add));

            var addReplacementMethod = AccessTools.Method(
                typeof(JobGiver_CheckReload_DoReloadCheck),
                nameof(AddPrimaryAndExtraGun),
                new[]
                {
            typeof(List<ThingWithComps>),
            typeof(ThingWithComps),
            typeof(Pawn)
                });

            // --------------------------------------------------------------------
            // Verifications
            // --------------------------------------------------------------------

            if (opportunisticReloadGetter == null)
                throw new Exception("Could not find CompAmmoUser.IsOpportunisticReloadActive getter.");

            if (opportunisticReloadReplacement == null)
                throw new Exception("Could not find ModifyIsOpportunisticReloadActive.");

            if (hasMagazineGetter == null)
                throw new Exception("Could not find CompAmmoUser.HasMagazine getter.");

            if (hasMagazineReplacement == null)
                throw new Exception("Could not find ModifyHasMagazine.");

            if (addMethod == null)
                throw new Exception("Could not find List<ThingWithComps>.Add.");

            if (addReplacementMethod == null)
                throw new Exception("Could not find AddPrimaryAndExtraGun.");

            // --------------------------------------------------------------------
            // 1. Patch IsOpportunisticReloadActive
            //
            // Original IL logic :
            //
            //     [tmpComp]
            //     call get_IsOpportunisticReloadActive
            //     brtrue / brfalse
            //
            // Becomes :
            //
            //     [tmpComp]
            //     call get_IsOpportunisticReloadActive
            //     ldarg.1               // pawn
            //     call ModifyIsOpportunisticReloadActive
            //     brtrue / brfalse
            //
            // So helper get :
            //     originalValue
            //     pawn
            // --------------------------------------------------------------------

            int opportunisticIndex = codes.FindIndex(
                code => code.Calls(opportunisticReloadGetter));

            if (opportunisticIndex < 0)
                throw new Exception(
                    "Could not find CompAmmoUser.IsOpportunisticReloadActive in DoReloadCheck.");

            codes.Insert(
                opportunisticIndex + 1,
                new CodeInstruction(OpCodes.Ldarg_1));

            codes.Insert(
                opportunisticIndex + 2,
                new CodeInstruction(
                    OpCodes.Call,
                    opportunisticReloadReplacement));

            // --------------------------------------------------------------------
            // 2. Patch HasMagazine
            //
            // IMPORTANT :
            // There are several HasMagazine calls
            // We take the first
            //
            // Original :
            //
            //     [tmpComp]
            //     call get_HasMagazine
            //     brfalse
            //
            // Devient :
            //
            //     [tmpComp]
            //     pop
            //     ldarg.1
            //     call ModifyHasMagazine
            //     brfalse
            //
            // Then the helper only has Pawn as param
            // --------------------------------------------------------------------

            int hasMagazineIndex = codes.FindIndex(
                code => code.Calls(hasMagazineGetter));

            if (hasMagazineIndex < 0)
                throw new Exception(
                    "Could not find CompAmmoUser.HasMagazine in DoReloadCheck.");

            // On consomme tmpComp qui était destiné au getter original.
            var popInstruction = new CodeInstruction(codes[hasMagazineIndex])
            {
                opcode = OpCodes.Pop,
                operand = null
            };

            codes[hasMagazineIndex] = popInstruction;

            codes.Insert(
                hasMagazineIndex + 1,
                new CodeInstruction(OpCodes.Ldarg_1));

            codes.Insert(
                hasMagazineIndex + 2,
                new CodeInstruction(
                    OpCodes.Call,
                    hasMagazineReplacement));

            // --------------------------------------------------------------------
            // 3. Patch guns.Add(...)
            // --------------------------------------------------------------------

            int addIndex = codes.FindIndex(
                code => code.Calls(addMethod));

            if (addIndex < 0)
                throw new Exception(
                    "Could not find List<ThingWithComps>.Add in JobGiver_CheckReload.DoReloadCheck.");

            // Original :
            //
            //     [guns, primary]
            //     call List<ThingWithComps>.Add
            //
            // Became :
            //
            //     [guns, primary]
            //     ldarg.1
            //     call AddPrimaryAndExtraGun
            //

            var loadPawnInstruction = new CodeInstruction(codes[addIndex])
            {
                opcode = OpCodes.Ldarg_1,
                operand = null
            };

            codes[addIndex] = loadPawnInstruction;

            codes.Insert(
                addIndex + 1,
                new CodeInstruction(
                    OpCodes.Call,
                    addReplacementMethod));

            return codes;
        }

        public static bool ModifyIsOpportunisticReloadActive(bool originalValue, Pawn pawn)
        {
            bool offHandIsOpportunisticReloadActive = false;
            if (pawn.equipment.TryGetOffHandEquipment(out ThingWithComps offHandEquip))
            {
                offHandIsOpportunisticReloadActive = offHandEquip.TryGetComp<CompAmmoUser>()?.IsOpportunisticReloadActive ?? false;
            }
            return originalValue || offHandIsOpportunisticReloadActive;
        }

        public static bool ModifyHasMagazine(Pawn pawn)
        {
            if (pawn == null)
                return false;

            CompAmmoUser mainComp = pawn.equipment?.Primary?.TryGetComp<CompAmmoUser>();

            if (mainComp == null)
                return false;

            bool oneEquipementHasMagazine = mainComp.HasMagazine;

            if (pawn.equipment.TryGetOffHandEquipment(out ThingWithComps offHandEquip))
            {
                oneEquipementHasMagazine |= offHandEquip.TryGetComp<CompAmmoUser>()?.HasMagazine ?? false;
            }

            return oneEquipementHasMagazine;
        }

        public static void AddPrimaryAndExtraGun(
           List<ThingWithComps> guns,
           ThingWithComps primary,
           Pawn pawn)
        {
            // If we're not checking a offHand, check primary again, as we may have altered its check
            if (!primary.IsOffHand() && CheckIfWeaponShouldBeAdded(pawn, primary))
            {
                guns.Add(primary);
            }

            // now check off hand
            if (pawn.equipment.TryGetOffHandEquipment(out ThingWithComps offHandEquip))
            {
                if (CheckIfWeaponShouldBeAdded(pawn, offHandEquip))
                {
                    guns.Add(offHandEquip);
                }
            }
        }

        private static bool CheckIfWeaponShouldBeAdded(Pawn pawn, ThingWithComps equip)
        {
            if (equip != null)
            {
                CompAmmoUser comp = equip.TryGetComp<CompAmmoUser>();
                if (comp != null)
                {
                    if (!comp.IsOpportunisticReloadActive)
                    {
                        return false;
                    }

                    if (Find.TickManager.TicksGame - pawn.LastAttackTargetTick < comp.MinimalTicksAfterFight)
                    {
                        return false;
                    }

                    if (!comp.HasMagazine)
                    {
                        return false;
                    }

                    var enemiesAround = pawn.Map.mapPawns.AllPawnsSpawned.Where(x => x.Position.InHorDistOf(pawn.Position, comp.SafeDistanceToReload) && !x.IsPsychologicallyInvisible() && x.HostileTo(pawn));
                    if (enemiesAround.Any())
                    {
                        return false;
                    }

                    return true;
                }
            }
            return false;
        }
    }
}
