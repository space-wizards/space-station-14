using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.Hands;

[TestFixture]
public sealed class HandForceDropTests: InteractionTest
{

    private const string Crowbar1 = "Crowbar1";
    private const string UnremovableCrowbar = "UnremovableCrowbar";
    private const string UnremovableDeleteOnDropCrowbar = "UnremovableDeleteOnDropCrowbar";
    private const string TestForceDropInContainerTestBox = "TestForceDropInContainerTestBox";
    private const string TestNoCapacityContainer = "TestNoCapacityContainer";
    
    [TestPrototypes] private const string Prototypes = $@"
- type: entity
  parent: BaseItem
  id: {Crowbar1}
  components:
  - type: Sprite
    sprite: Objects/Tools/crowbar.rsi
    state: icon

- type: entity
  parent: {Crowbar1}
  id: {UnremovableCrowbar}
  components:
  - type: Unremoveable
    deleteOnDrop: false

- type: entity
  parent: {Crowbar1}
  id: {UnremovableDeleteOnDropCrowbar}
  components:
  - type: Unremoveable
    deleteOnDrop: true

- type: entity
  id: {TestForceDropInContainerTestBox}
  name: box
  components:
  - type: EntityStorage
  - type: ContainerContainer
    containers:
      entity_storage: !type:Container

- type: entity
  id: {TestNoCapacityContainer}
  name: box0
  components:
  - type: EntityStorage
    capacity: 0
  - type: ContainerContainer
    containers:
      entity_storage: !type:Container
";

    [Test]
    public async Task TestTryDropUnremovable()
    {
        
        var initialFreeHands = HandSys.CountFreeHands(SPlayer);
        Assume.That(initialFreeHands, Is.GreaterThan(0), "SPlayer must have at least one free hand");

        await PlaceInHands(UnremovableCrowbar);
        
        var holdingItemFreeHands = HandSys.CountFreeHands(SPlayer);
        Assume.That(holdingItemFreeHands, Is.LessThan(initialFreeHands), "SPlayer somehow has more free hands after being given the item");
        
        Assert.That(HandSys.TryDrop(SPlayer, force: false), Is.False, "SPlayer was somehow able to drop the unremovable item without it being forced");
        Assert.That(HandSys.CountFreeHands(SPlayer), Is.EqualTo(holdingItemFreeHands), "SPlayer somehow freed a hand after unforced drop of unremovable item.");
        
        Assert.That(HandSys.TryDrop(SPlayer, force:true), Is.True, "SPlayer was unable to drop the item despite being forced to drop it");
        Assert.That(HandSys.CountFreeHands(SPlayer), Is.EqualTo(initialFreeHands), "SPlayer did not return to initial free hand count after forcibly dropping the item!");
    }

    [Test]
    public async Task TryForceDropIntoContainer_CantInsert()
    {
        // This test doesn't seem to work.
        // Might remove 'forceDrop' from 'TryDropIntoContainer', seeing as I'm struggling to test it
        var _entStorageSys = Server.System<EntityStorageSystem>();
        var _containerSys = Server.System<SharedContainerSystem>();
        
        // we create the box
        EntityUid boxUid = default;
        await Server.WaitPost(() =>
            boxUid = SEntMan.SpawnEntity(TestNoCapacityContainer, SEntMan.GetCoordinates(PlayerCoords)));
        Assert.That(SEntMan.TryGetComponent<EntityStorageComponent>(boxUid, out var entityStorage), Is.True, "Unable to get entity storage of the box!");
        
        //await Server.WaitPost(() => _entStorageSys.OpenStorage(boxUid) );
        
        
        // make sure that an item can't get inserted into the box regularly
        await Server.WaitPost(() => 
        {
            var randomCrowbar = SEntMan.SpawnInContainerOrDrop(Crowbar1, boxUid, entityStorage!.Contents.ID, out bool randomCrowbarInserted);
            
            Assume.That(_containerSys.IsEntityInContainer(randomCrowbar), Is.False,"random crowbar should not be in the container!");
            Assume.That(randomCrowbarInserted, Is.False,"The random crowbar somehow got inserted into the zero-capacity box");
            //SEntMan.DeleteEntity(randomCrowbar);
        });
        await Server.WaitRunTicks(1); // let the random crowbar despawn
        Assume.That(entityStorage!.Contents.ContainedEntities, Is.Empty,"Entity storage was not initially empty!");
        // put item into hands
        var netItem = await PlaceInHands(UnremovableCrowbar);
        var item = SEntMan.GetEntity(netItem);

        Assume.That(HandSys.GetActiveItemOrSelf(SPlayer), Is.EqualTo(item),
            "SPlayer is not holding the item in active hand");

        await Server.WaitPost(() =>
        {
            Assert.That(HandSys.TryDropIntoContainer(SPlayer, item, entityStorage!.Contents, forceDrop: true), Is.True,
                "Forcing TryDrop did not return true (supposed to return true if item can be removed from hands)");
        });
        
        
        Assert.That(entityStorage.Contents.ContainedEntities, Is.Empty,
            "Entity storage contained an item even though an item wasn't supposed to have been inserted!");
        Assert.That(_containerSys.IsEntityInContainer(item), Is.False,"Item should not be in the container!");
    }
}