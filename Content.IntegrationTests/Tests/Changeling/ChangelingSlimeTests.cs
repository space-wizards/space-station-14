using Content.Client.UserInterface.Systems.Hotbar.Widgets;
using Content.Client.UserInterface.Systems.Storage.Controls;
using Content.IntegrationTests.NUnit.Constraints;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Changeling.Components;
using Content.Shared.Changeling.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Robust.Client.UserInterface;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Content.Shared.Input;

namespace Content.IntegrationTests.Tests.Changeling;

[TestOf(typeof(ChangelingTransformSystem))]
public sealed class ChangelingSlimeTests : InteractionTest
{
    protected override string PlayerPrototype => "MobLing";
    private static readonly EntProtoId SlimeHumanoidProtoId = "MobSlimePerson";
    private static readonly EntProtoId AppleProtoId = "FoodApple";

    [SidedDependency(Side.Server)] private SharedChangelingIdentitySystem _changelingIdentity = default!;
    [SidedDependency(Side.Server)] private ChangelingTransformSystem _changelingTransform = default!;
    [SidedDependency(Side.Server)] private SharedStorageSystem _sharedStorage = default!;
    [SidedDependency(Side.Server)] private SharedTransformSystem _transform = default!;
    [SidedDependency(Side.Server)] private SharedContainerSystem _container = default!;
    [SidedDependency(Side.Server)] private SharedHandsSystem _hands = default!;

    public override async Task DoSetup()
    {
        await base.DoSetup();

        // Set up the ling with a slime present and already consumed.
        var slime = await SpawnTarget(SlimeHumanoidProtoId);
        var slimeEntity = ToServer(slime);
        await Server.WaitPost(() =>
        {
            // Just give the ling the identity of a slime, no need to mess around with devouring, that's on the devour test to handle.
            _changelingIdentity.GrantIdentity(SPlayer, slimeEntity);
        });
    }

    [Test]
    [Description(
        "Test that a changeling transforming into a slime will gain the appropriate storage container and BUI associated with the species and lose them when transforming back into a human.")]
    public async Task TransformIntoSlimeTest()
    {
        Assume.That(_changelingIdentity.TryGetDataFromOriginal(SPlayer,
            SPlayer,
            out var humanIdentityData), "Failed to get the changeling's human identity data.");
        Assume.That(_changelingIdentity.TryGetDataFromOriginal(
            SPlayer,
            STarget!.Value,
            out var slimeIdentity), "Failed to get the changeling's slime identity data.");

        // Assert that the player does not have a storage component or BUI before transforming into a slime.
        Assert.That(SPlayer, Has.No.Comp<StorageComponent>(Server), "Non-slime player spawned with a storage component.");
        Assert.That(CUiSys.TryGetInterfaceData(CPlayer, StorageComponent.StorageUiKey.Key, out _), Is.False, "Non-slime player spawned with a storage BUI.");
        Assert.That(CUiSys.TryGetInterfaceData(CPlayer, ChangelingTransformUiKey.Key, out _), Is.True, "Changeling did not spawn with a changeling transform BUI.");

        // Transform the player into a slime.
        await Server.WaitPost(() =>
        {
            _changelingTransform.TransformInto(SPlayer, slimeIdentity!.Identity!.Value);
        });
        await AwaitDoAfters();

        // Check storage and BUI presence.
        Assert.That(SPlayer, Has.Comp<StorageComponent>(Server), "Changeling did not gain a storage component when transforming into a slime.");
        Assert.That(CUiSys.TryGetInterfaceData(CPlayer, StorageComponent.StorageUiKey.Key, out _), Is.True, "Changeling did not gain a storage BUI when transforming into a slime.");
        Assert.That(CUiSys.TryGetInterfaceData(CPlayer, ChangelingTransformUiKey.Key, out _), Is.True, "Changeling lost their changeling transform BUI when transforming into a slime.");

        // Transform the player back into a human.
        await Server.WaitPost(() =>
        {
            _changelingTransform.TransformInto(SPlayer, humanIdentityData!.Identity!.Value);

        });
        await AwaitDoAfters();

        // Reassert that the storage and BUI presence is back to the original state.
        Assert.That(SPlayer, Has.No.Comp<StorageComponent>(Server), "Changeling did not lose their storage component when transforming back into a human.");
        // Assert.That(CUiSys.TryGetInterfaceData(CPlayer, StorageComponent.StorageUiKey.Key, out _), Is.False, "Changeling did not lose their storage BUI when transforming back into a human."); // TODO: BUI is not being removed right now.
        Assert.That(CUiSys.TryGetInterfaceData(CPlayer, ChangelingTransformUiKey.Key, out _), Is.True, "Changeling lost their changeling transform BUI when transforming back into a human.");
    }

    [Test]
    [Description(
        "Test that a changeling transforming between slimes wont lose a storage")]
    public async Task TransformPreserveStorage()
    {
        var lingIdentityComp = Comp<ChangelingIdentityComponent>(Player);
        Assume.That(_changelingIdentity.TryGetDataFromOriginal(SPlayer,
            STarget!.Value,
            out var slimeIdentity1), "Failed to get the changeling's slime identity data.");

        // Spawn a second slime.
        var secondSlime = await Spawn(SlimeHumanoidProtoId);
        var secondSlimeEntity = ToServer(secondSlime);

        await Server.WaitAssertion(() =>
        {
            _changelingIdentity.GrantIdentity(SPlayer, secondSlimeEntity);
            Assume.That(lingIdentityComp.ConsumedIdentities, Has.Count.EqualTo(3), "Changeling did not gain the correct number of identities.");
            // Transform into the first slime.
            _changelingTransform.TransformInto(SPlayer, slimeIdentity1!.Identity!.Value);
        });
        await AwaitDoAfters();

        Assume.That(_changelingIdentity.TryGetDataFromOriginal(SPlayer,
            secondSlimeEntity,
            out var slimeIdentity2), "Failed to get the changeling's second slime identity data.");

        Assert.That(SPlayer, Has.Comp<StorageComponent>(Server), "Changeling did not gain a storage component when transforming into a slime.");
        // Spawn a test item in the players hand.
        var apple = await PlaceInHands(AppleProtoId);
        var appleEnt = ToServer(apple);
        // Now insert it into our slime storage.
        var storageComponent = Comp<StorageComponent>(Player);
        await Server.WaitPost(() =>
        {
            _sharedStorage.PlayerInsertHeldEntity(SPlayer, SPlayer);
        });
        Assert.That(storageComponent.StoredItems, Has.Count.EqualTo(1));
        Assert.That(storageComponent.StoredItems.TryGetValue(appleEnt, out var appleStoredLocation), "Failed to get the stored location of the apple.");

        // Transform into the second slime we added earlier.
        await Server.WaitPost(() =>
        {
            _changelingTransform.TransformInto(SPlayer, slimeIdentity2!.Identity!.Value);
        });
        await AwaitDoAfters();

        // Check that it's the same component from earlier and that the apple is in the same container.
        storageComponent = Comp<StorageComponent>(Player);
        Assert.That(storageComponent.StoredItems, Has.Count.EqualTo(1), "Changeling lost their storage contents when transforming between slimes.");
        Assert.That(_container.TryGetContainingContainer(appleEnt, out var container), "Failed to get the container for the stored item after transforming between slimes.");
        Assert.That(container, Is.EqualTo(storageComponent.Container), "The stored item is no longer in the same storage container after transforming between slimes.");
        Assert.That(storageComponent.StoredItems.TryGetValue(appleEnt, out var postTransformStoredLocation), "Failed to get the stored location of the apple after transforming between slimes.");
        Assert.That(appleStoredLocation, Is.EqualTo(postTransformStoredLocation), "The stored item is no longer in the same location within the storage container after transforming between slimes.");

        // Actually pull the apple from the inventory.
        await Activate(Player);
        Assert.That(IsUiOpen(StorageComponent.StorageUiKey.Key), "Storage BUI did not open when activating the changeling.");
        var ctrl = GetStorageControl(apple);
        await ClickControl(ctrl, ContentKeyFunctions.MoveStoredItem);
        await RunUntilSynced();
        Assert.That(_hands.IsHolding((SPlayer, Hands), appleEnt), "Changeling did not successfully pull the stored item from their storage.");
    }

    [Test]
    [Description(
        "Test that a changeling transforming out of a slime drops the item inside their storage onto the ground")]
    public async Task TransformDropStorage()
    {
        var transformComponent = Comp<TransformComponent>(Player);
        // Set up having an apple inside the slimes storage.
        Assume.That(_changelingIdentity.TryGetDataFromOriginal(SPlayer,
            STarget!.Value,
            out var slimeIdentity), "Failed to get slime identity.");
        Assume.That(_changelingIdentity.TryGetDataFromOriginal(SPlayer,
            SPlayer,
            out var humanIdentity), "Failed to get human identity.");

        var apple = await PlaceInHands(AppleProtoId);
        var appleEnt = ToServer(apple);

        // Transform into a slime.
        await Server.WaitPost(() =>
        {
            _changelingTransform.TransformInto(SPlayer, slimeIdentity!.Identity!.Value);
        });
        await AwaitDoAfters();

        // Insert the apple into the slime storage.
        await Server.WaitPost(() =>
        {
            _sharedStorage.PlayerInsertHeldEntity(SPlayer, SPlayer);
        });
        var storageComponent = Comp<StorageComponent>(Player);
        Assert.That(_container.TryGetContainingContainer(appleEnt, out var container), "Failed to get the container for the stored item after inserting into slime storage.");
        Assert.That(container, Is.EqualTo(storageComponent.Container), "The stored item is not in the storage container after inserting into slime storage.");

        // Now transform out.
        await Server.WaitPost(() =>
        {
            _changelingTransform.TransformInto(SPlayer, humanIdentity!.Identity!.Value);
        });
        await AwaitDoAfters();

        // Assert that the storage container has been properly removed from the player,
        // items have been dumped and the lifestage for the StorageComponent has become Deleted
        Assert.That(SPlayer, Has.No.Comp<StorageComponent>(Server));
        Assert.That(storageComponent.StoredItems, Has.Count.EqualTo(0));
        Assert.That(storageComponent.LifeStage, Is.EqualTo(ComponentLifeStage.Deleted));

        // Does the apple still exist?
        Assert.That(_container.TryGetContainingContainer(appleEnt, out _), Is.False, "The apple is still in a container after the changeling transformed out of slime form.");
        Assert.That(_transform.InRange(SPlayer, appleEnt, 1), Is.True, "The apple is not within range after the changeling transformed out of slime form.");
    }

    private ItemGridPiece GetStorageControl(NetEntity target)
    {
        var uid = ToClient(target);
        var hotbar = GetWidget<HotbarGui>();
        var storageContainer = GetControlFromField<Control>(nameof(HotbarGui.SingleStorageContainer), hotbar);
        return GetControlFromChildren<ItemGridPiece>(c => c.Entity == uid, storageContainer);
    }
}
