using CombatExtended;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using Verse.Sound;

namespace DualWield.CECompat.Harmony
{
    [HarmonyPatch(typeof(ITab_Inventory), nameof(ITab_Inventory.DrawThingRowCE))]
    public static class ITab_Inventory_DrawThingRowCE
    {
        private static readonly MethodInfo BeforeWornApparelMethod =
            AccessTools.Method(
                typeof(ITab_Inventory_DrawThingRowCE),
                nameof(AddOffHandButton));

        // We gonna add a float menu just after the primary equip which is just before the CE_ReloadApparel button in the function
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions,
            MethodBase __originalMethod)
        {
            var matcherFloatOptionList = new CodeMatcher(instructions);

            // Get the original "thing" argument index.
            var thingParameterIndex = __originalMethod
                .GetParameters()
                .Single(p => p.ParameterType == typeof(Thing))
                .Position + 1; // +1 because instance methods have "this" at argument 0.

            var listCtor = AccessTools.Constructor(
                typeof(List<FloatMenuOption>),
                Type.EmptyTypes);

            // Find:
            //
            // new List<FloatMenuOption>()
            // stloc floatOptionList
            //
            // and keep the local used by floatOptionList.
            matcherFloatOptionList
                .MatchStartForward(
                    new CodeMatch(OpCodes.Newobj, listCtor),
                    new CodeMatch(ci => ci.IsStloc()))
                .ThrowIfInvalid("Could not find floatOptionList initialization.");

            var floatOptionListLocal = matcherFloatOptionList.Advance(1).Instruction.operand;

            var matcher = new CodeMatcher(instructions);

            // Find:
            //
            // call get_WornApparel
            //
            // and insert our call immediately before it.
            matcher
                .Start()
                .MatchStartForward(
                    CodeMatch.Calls(
                        AccessTools.PropertyGetter(
                            typeof(Pawn_ApparelTracker),
                            nameof(Pawn_ApparelTracker.WornApparel)))
                    )
                .ThrowIfInvalid("Could not find get_WornApparel.");

            var injectedInstructions = new[]
               {
                    CodeInstruction.LoadArgument(0),
                    new CodeInstruction(OpCodes.Ldloc, floatOptionListLocal),
                    CodeInstruction.LoadArgument(thingParameterIndex),
                    new CodeInstruction(OpCodes.Call, BeforeWornApparelMethod)
                };

            // get_WornApparel is the target of a conditional branch.
            // Move its labels to the first injected instruction so the branch
            // executes the injected code instead of skipping it.
            injectedInstructions[0].MoveLabelsFrom(matcher.Instruction);

            matcher.Insert(injectedInstructions);

            return matcher.Instructions();
        }

        // Add offhand button on inventory if needed
        private static void AddOffHandButton(
            ITab_Inventory instance,
            List<FloatMenuOption> floatOptionList,
            Thing thing)
        {

            var SelPawnForGear = AccessTools.Property(typeof(ITab_Inventory), "SelPawnForGear")
                       .GetValue(instance) as Pawn;

            // Cannot be equipped
            ThingWithComps eq = thing as ThingWithComps;
            if (eq?.TryGetComp<CompEquippable>() == null)
            {
                return;
            }

            // Not inventory
            CompInventory compInventory = SelPawnForGear.TryGetComp<CompInventory>();
            if (compInventory == null)
            {
                return;
            }

            // Cannot be equipped for pawn
            FloatMenuOption equipOption;
            string eqLabel = GenLabel.ThingLabel(eq.def, eq.Stuff, 1);
            if (!EquipmentUtility.CanEquip(eq, SelPawnForGear, out var reason))
            {
                return;
            }

            // is quest forbidden
            if (SelPawnForGear.IsQuestLodger() && !EquipmentUtility.QuestLodgerCanEquip(eq, SelPawnForGear))
            {
                return;
            }

            // Can be put away (already set in the original method)
            if (SelPawnForGear.equipment.AllEquipmentListForReading.Contains(eq) && SelPawnForGear.inventory != null)
            {
                return;
            }

            // Lets go we can add it off hand !
            if (SelPawnForGear.HasMissingArmOrHand())
            {
                equipOption = new FloatMenuOption("CannotEquip".Translate(thing.LabelShort) + " " + "DW_AsOffHand".Translate() + " (" + "DW_MissArmOrHand".Translate() + ")", null);
            }
            else if (SelPawnForGear.equipment != null && SelPawnForGear.equipment.Primary != null && SelPawnForGear.equipment.Primary.def.IsTwoHand())
            {
                equipOption = new FloatMenuOption("CannotEquip".Translate(thing.LabelShort) + " " + "DW_AsOffHand".Translate() + " (" + "DW_WieldingTwoHanded".Translate() + ")", null);
            }
            else if (thing.def.IsTwoHand())
            {
                equipOption = new FloatMenuOption("CannotEquip".Translate(thing.LabelShort) + " " + "DW_AsOffHand".Translate() + " (" + "DW_NoTwoHandedInOffHand".Translate() + ")", null);
            }
            else if (!thing.def.CanBeOffHand())
            {
                equipOption = new FloatMenuOption("CannotEquip".Translate(thing.LabelShort) + " " + "DW_AsOffHand".Translate() + " (" + "DW_CannotBeOffHand".Translate() + ")", null);
            }
            // new rules : if there is a shield, we can't equip off hand
            else if (CE_Utility.HasShield(SelPawnForGear))
            {
                equipOption = new FloatMenuOption("CannotEquip".Translate(thing.LabelShort) + " " + "DW_AsOffHand".Translate() + " (" + "DW_OffHandWithShield".Translate() + ")", null);
            }
            else
            {
                string equipOptionLabel = "DW_EquipOffHand".Translate(eqLabel);
                if (eq.def.IsRangedWeapon && SelPawnForGear.story != null && SelPawnForGear.story.traits.HasTrait(TraitDefOf.Brawler))
                {
                    equipOptionLabel = equipOptionLabel + " " + "EquipWarningBrawler".Translate();
                }
                equipOption = new FloatMenuOption(
                                equipOptionLabel,
                                (SelPawnForGear.story != null && SelPawnForGear.WorkTagIsDisabled(WorkTags.Violent))
                                ? null
                                : new Action(() =>
                                    {
                                        ThingWithComps newEq = (ThingWithComps)compInventory.container.Take(eq, 1);
                                        SelPawnForGear.equipment.MakeRoomForOffHand(newEq);
                                        SelPawnForGear.equipment.AddOffHandEquipment(newEq);
                                        if (eq.def.soundInteract != null)
                                        {
                                            eq.def.soundInteract.PlayOneShot(new TargetInfo(compInventory.parent.Position, compInventory.parent.MapHeld, false));
                                        }
                                    }));

            }
            floatOptionList.Add(equipOption);
        }
    }

    [HarmonyPatch(typeof(ITab_Inventory), "SyncedTryTransferEquipmentToContainer")]
    public static class ITab_Inventory_SyncedTryTransferEquipmentToContainer
    {
        public static void Prefix(Pawn p)
        {
            // Put away remove all weapon
            if (!p.equipment.Primary.IsOffHand() &&  p.equipment.TryGetOffHandEquipment(out ThingWithComps offHandEquip))
            {
                p.equipment.TryTransferEquipmentToContainer(offHandEquip, p.inventory.innerContainer);
            }
        }
    }
}
