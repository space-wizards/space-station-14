using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Actions;
using Content.Server.Inventory;
using Content.Server.Spawners.Components;
using Content.Shared.Actions.Components;
using Content.Shared.Clothing.Components;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Components;
using Content.Shared.Prototypes;
using Content.Shared.Storage.Components;
using Content.Shared.Tag;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Inventory;

/// <summary>
/// This test spawns in every item that has a ClothingComponent and then simulates:
/// 1. Equipping it (and jumpsuit/hardsuit if needed)
/// 2. Triggering any InstantActions it grants.
/// 3. Unequipping it.
/// </summary>
public sealed class ClothingEquipTest : InteractionTest
{
    private const string HardsuitProto = "ClothingOuterHardsuitEVA";
    private const string JumpsuitProto = "ClothingUniformJumpsuitColorGrey";

    private readonly List<Type> _ignoredComponents =
    [
        // Mobs are a massive pain in this test
        // (Admeme mouse eating your cak, cancer mouse killing you... mice just dying...)
        typeof(MobStateComponent),
        typeof(RandomSpawnerComponent),
    ];

    private readonly List<ProtoId<TagPrototype>> _ignoredTags =
    [
        "PetOnly",
    ];

    private readonly List<Type> _removedComponents =
    [
        // Material bag can pick up other clothing items.
        typeof(MagnetPickupComponent),
    ];

    protected override string PlayerPrototype => "MobHuman";

    [Test]
    public async Task EquipAllClothingTest()
    {
        var clothingProtos = ProtoMan.EnumeratePrototypes<EntityPrototype>()
            .Where(x => x.HasComponent<ClothingComponent>()
                        && !x.HideSpawnMenu
                        && !x.Abstract
                        && !Pair.IsTestPrototype(x)
                        && !(x.TryGetComponent<TagComponent>(out var tags, Factory)
                             && Tags.HasAnyTag(tags, _ignoredTags))
                        && !_ignoredComponents.Any(ignore => x.HasComponent(ignore)));

        List<EntityUid> clothings = [];
        EntityUid jumpsuit = default!;
        EntityUid hardsuit = default!;
        await Server.WaitPost(() =>
        {
            foreach (var clothing in clothingProtos)
            {
                var ent = SEntMan.SpawnAtPosition(clothing.ID, ToServer(PlayerCoords));
                clothings.Add(ent);

                _removedComponents.ForEach(remove => SEntMan.RemoveComponent(ent, remove));
            }

            jumpsuit = SEntMan.SpawnAtPosition(JumpsuitProto, ToServer(PlayerCoords));
            hardsuit = SEntMan.SpawnAtPosition(HardsuitProto, ToServer(PlayerCoords));
        });

        await AddAtmosphere();

        var invSystem = Server.System<ServerInventorySystem>();
        var invComp = SEntMan.GetComponent<InventoryComponent>(SPlayer);

        var actionSys = Server.System<ActionsSystem>();
        var baseActions = actionSys.GetActions(SPlayer).ToList();

        foreach (var clothing in clothings)
        {
            Assert.That(SEntMan.TryGetComponent<ClothingComponent>(clothing, out var clothingComp),
                $"{SEntMan.ToPrettyString(clothing)} doesn't have ClothingComponent");
            foreach (var slot in invComp.Slots)
            {
                if ((slot.SlotFlags & clothingComp!.Slots) == 0x0)
                    continue;

                await Server.WaitAssertion(() =>
                {
                    if (slot.SlotFlags is SlotFlags.IDCARD or SlotFlags.POCKET)
                        DoEquip(invSystem, jumpsuit, "jumpsuit");

                    if (slot.SlotFlags == SlotFlags.SUITSTORAGE)
                        DoEquip(invSystem, hardsuit, "outerClothing");

                    DoEquip(invSystem, clothing, slot.Name, clothingComp);
                });
                await RunTicks(1);

                var newActions = actionSys.GetActions(SPlayer)
                    .Except(baseActions)
                    // Don't bother with more complicated actions (they can take targets, etc.)
                    .Where(action => SEntMan.HasComponent<InstantActionComponent>(action.Owner))
                    .ToList();

                await Server.WaitPost(() => RunActions(newActions, actionSys));
                await RunTicks(1);

                var unremovableComp = SEntMan.GetComponentOrNull<UnremoveableComponent>(clothing);
                // Electropack or similar has to be force removed, since our unequip actor is always ourselves
                var needsForce = SEntMan.HasComponent<SelfUnremovableClothingComponent>(clothing);

                if (unremovableComp != null)
                {
                    needsForce = true;
                    // We want to reuse this entity for other slots. (E.g., CluwnePDA)
                    unremovableComp.DeleteOnDrop = false;
                }

                var isRemoved = false;
                await Server.WaitPost(() =>
                {
                    // Try without forcing first
                    isRemoved = DoUnequip(invSystem, clothing, slot.Name, false, clothingComp, false);

                    // If we should've needed to force for this item, but got it off, that's no good.
                    Assert.That(isRemoved && needsForce,
                        Is.False,
                        $"Unequipped {SEntMan.ToPrettyString(clothing)}, which should have been unremovable.");

                    // Just in case some kind of toggle action put us into a state that this test doesn't expect.
                    // Currently this doesn't seem to get hit.
                    if (!isRemoved)
                        RunActions(newActions, actionSys);
                });
                await RunTicks(1);

                await Server.WaitAssertion(() =>
                {
                    if (!isRemoved)
                        DoUnequip(invSystem, clothing, slot.Name, needsForce, clothingComp);

                    if (slot.SlotFlags is SlotFlags.IDCARD or SlotFlags.POCKET)
                        DoUnequip(invSystem, jumpsuit, "jumpsuit");

                    if (slot.SlotFlags == SlotFlags.SUITSTORAGE)
                        DoUnequip(invSystem, hardsuit, "outerClothing");
                });
                await RunTicks(1);
            }
        }
    }

    private void DoEquip(ServerInventorySystem invSystem,
        EntityUid clothing,
        string slot,
        ClothingComponent clothingComp = null,
        bool assert = true)
    {
        var equipped = invSystem.TryEquip(SPlayer, clothing, slot, clothing: clothingComp);
        if (assert)
            Assert.That(equipped, $"Failed to equip {SEntMan.ToPrettyString(clothing)} to {slot}");
    }

    private bool DoUnequip(ServerInventorySystem invSystem,
        EntityUid clothing,
        string slot,
        bool force = false,
        ClothingComponent clothingComp = null,
        bool assert = true)
    {
        var equipped = invSystem.TryUnequip(SPlayer, SPlayer, slot, force: force, clothing: clothingComp);
        if (assert)
            Assert.That(equipped, $"Failed to unequip {SEntMan.ToPrettyString(clothing)} to {slot}");
        return equipped;
    }

    private void RunActions(List<Entity<ActionComponent>> actions, ActionsSystem actionSys)
    {
        foreach (var action in actions)
        {
            actionSys.PerformAction(SPlayer, action);
        }
    }
}
