using System.Numerics;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Charges.Systems;
using Content.Shared.RCD;
using Content.Shared.RCD.Components;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Construction;

public sealed class RCDTest : InteractionTest
{
    private static readonly EntProtoId RCDProtoId = "RCD";
    private static readonly ProtoId<RCDPrototype> RCDSettingWall = "WallSolid";
    private static readonly ProtoId<RCDPrototype> RCDSettingAirlock = "Airlock";
    private static readonly ProtoId<RCDPrototype> RCDSettingPlating = "Plating";
    private static readonly ProtoId<RCDPrototype> RCDSettingFloorSteel = "FloorSteel";
    private static readonly ProtoId<RCDPrototype> RCDSettingDeconstruct = "Deconstruct";
    private static readonly ProtoId<RCDPrototype> RCDSettingDeconstructTile = "DeconstructTile";
    private static readonly ProtoId<RCDPrototype> RCDSettingDeconstructLattice = "DeconstructLattice";

    /// <summary>
    /// Tests RCD construction and deconstruction, as well as selecting options from the radial menu.
    /// </summary>
    [Test]
    public async Task RCDConstructionDeconstructionTest()
    {
        // Place some tiles around the player so that we have space to build.
        var pCoords = SEntMan.GetCoordinates(PlayerCoords);
        var pNorth = pCoords.Offset(new Vector2(0, 1));
        var pSouth = pCoords.Offset(new Vector2(0, -1));
        var pEast = pCoords.Offset(new Vector2(1, 0));
        var pWest = pCoords.Offset(new Vector2(-1, 0));
        await SetTile(Plating, SEntMan.GetNetCoordinates(pNorth), MapData.Grid);
        await SetTile(Plating, SEntMan.GetNetCoordinates(pSouth), MapData.Grid);
        await SetTile(Plating, SEntMan.GetNetCoordinates(pEast), MapData.Grid);
        await SetTile(Lattice, SEntMan.GetNetCoordinates(pWest), MapData.Grid);

        var rcd = await PlaceInHands(RCDProtoId);

        // Check that the RCD spawned with charges.
        var sCharges = SEntMan.System<SharedChargesSystem>();
        var initialCharges = sCharges.GetCurrentCharges(ToServer(rcd));
        Assert.That(initialCharges, Is.GreaterThan(0), "RCD spawned without charges.");

        // Check if using the RCD opens the UI.
        Assert.That(IsUiOpen(RcdUiKey.Key), Is.False, "RCD UI was opened when picking it up.");
        await UseInHand();
        await RunTicks(3);
        /*
        Assert.That(IsUiOpen(RcdUiKey.Key), Is.True, "RCD UI was not opened when using the RCD while holding it.");

        // Simulating a click on the right control for nested radial menus is very complicated.
        // So we just manually send a networking message from the client to tell the server we selected an option.
        // TODO: Write a separate test for clicking through a SimpleRadialMenu.
        await SendBui(RcdUiKey.Key, new RCDSystemMessage(RCDSettingWall));
        await CloseBui(RcdUiKey.Key);
        Assert.That(IsUiOpen(RcdUiKey.Key), Is.False, "RCD UI is still open.");

        // Build a wall next to the player.
        await Interact(null, pNorth);

        // Check that there is exactly one wall.
        Assert.That(ProtoMan.TryIndex(RCDSettingWall, out var settingWall), "RCDPrototype not found.");
        Assert.That(settingWall.Prototype, Is.Not.Null, "RCDPrototype has a null spawning prototype.");
        await AssertEntityLookup((settingWall.Prototype, 1));

        // Check that the wall is in the correct tile.
        var wallUid = await FindEntity(settingWall.Prototype);
        AssertLocation(FromServer(wallUid), FromServer(pNorth));

        // Check that the cost of the wall was subtracted from the current charges.
        var newCharges = sCharges.GetCurrentCharges(ToServer(rcd));
        Assert.That(initialCharges - settingWall.Cost, Is.EqualTo(newCharges), "RCD has wrong amount of charges after building something.");
        initialCharges = newCharges;

        // Try building another wall in the same spot.
        await Interact(null, pNorth);

        // Check that there is still exactly one wall.
        await AssertEntityLookup((settingWall.Prototype, 1));

        // Check that the failed construction did not cost us any charges.
        newCharges = sCharges.GetCurrentCharges(ToServer(rcd));
        Assert.That(initialCharges, Is.EqualTo(newCharges), "RCD has wrong amount of charges after failing to build something.");

        // Switch to building airlocks.
        await UseInHand();
        await RunTicks(3);
        Assert.That(IsUiOpen(RcdUiKey.Key), Is.True, "RCD UI was not opened when using the RCD while holding it.");
        await SendBui(RcdUiKey.Key, new RCDSystemMessage(RCDSettingAirlock));
        await CloseBui(RcdUiKey.Key);
        Assert.That(IsUiOpen(RcdUiKey.Key), Is.False, "RCD UI is still open.");

        // Build an airlock next to the player.
        await Interact(null, pSouth);

        // Check that there is exactly one airlock.
        Assert.That(ProtoMan.TryIndex(RCDSettingAirlock, out var settingAirlock), "RCDPrototype not found.");
        Assert.That(settingAirlock.Prototype, Is.Not.Null, "RCDPrototype has a null spawning prototype.");
        await AssertEntityLookup((settingAirlock.Prototype, 1));

        // Check that the wall is in the correct tile.
        var airlockUid = await FindEntity(settingAirlock.Prototype);
        AssertLocation(FromServer(airlockUid), FromServer(pSouth));

        // Check that the cost of the airlock was subtracted from the current charges.
        newCharges = sCharges.GetCurrentCharges(ToServer(rcd));
        Assert.That(initialCharges - settingAirlock.Cost, Is.EqualTo(newCharges), "RCD has wrong amount of charges after building something.");
        initialCharges = newCharges;

        // Switch to building plating.
        await UseInHand();
        await RunTicks(3);
        Assert.That(IsUiOpen(RcdUiKey.Key), Is.True, "RCD UI was not opened when using the RCD while holding it.");
        await SendBui(RcdUiKey.Key, new RCDSystemMessage(RCDSettingPlating));
        await CloseBui(RcdUiKey.Key);
        Assert.That(IsUiOpen(RcdUiKey.Key), Is.False, "RCD UI is still open.");

        // Try building plating on existing plating.
        await Interact(null, pWest);

        // Check that the failed construction did not cost us any charges.
        newCharges = sCharges.GetCurrentCharges(ToServer(rcd));
        Assert.That(initialCharges, Is.EqualTo(newCharges), "RCD has wrong amount of charges after failing to build something.");

        // Check that the tile did not change.
        Assert.That(ProtoMan.TryIndex(RCDSettingAirlock, out var settingPlating), "RCDPrototype not found.");
        Assert.That(settingPlating.Prototype, Is.Not.Null, "RCDPrototype has a null spawning prototype.");
        await AssertTile(settingPlating.Prototype, FromServer(pWest));

        // Try building plating on top of lattice.
        await Interact(null, pEast);

        // Check that the cost of the plating was subtracted from the current charges.
        newCharges = sCharges.GetCurrentCharges(ToServer(rcd));
        Assert.That(initialCharges - settingPlating.Cost, Is.EqualTo(newCharges), "RCD has wrong amount of charges after building something.");
        initialCharges = newCharges;

        // Check that the tile is now plating.
        await AssertTile(settingPlating.Prototype, FromServer(pEast));

        // Switch to building steel tiles.
        await UseInHand();
        await RunTicks(3);
        Assert.That(IsUiOpen(RcdUiKey.Key), Is.True, "RCD UI was not opened when using the RCD while holding it.");
        await SendBui(RcdUiKey.Key, new RCDSystemMessage(RCDSettingFloorSteel));
        await CloseBui(RcdUiKey.Key);
        Assert.That(IsUiOpen(RcdUiKey.Key), Is.False, "RCD UI is still open.");

        // Try building a steel tile on top of plating.
        await Interact(null, pEast);

        // Check that the tile is now a steel tile.
        Assert.That(ProtoMan.TryIndex(RCDSettingFloorSteel, out var settingFloorSteel), "RCDPrototype not found.");
        Assert.That(settingFloorSteel.Prototype, Is.Not.Null, "RCDPrototype has a null spawning prototype.");
        await AssertTile(settingFloorSteel.Prototype, FromServer(pEast));

        // Check that the cost of the plating was subtracted from the current charges.
        newCharges = sCharges.GetCurrentCharges(ToServer(rcd));
        Assert.That(initialCharges - settingFloorSteel.Cost, Is.EqualTo(newCharges), "RCD has wrong amount of charges after building something.");
        initialCharges = newCharges;

        // Switch to deconstruction mode.
        await UseInHand();
        await RunTicks(3);
        Assert.That(IsUiOpen(RcdUiKey.Key), Is.True, "RCD UI was not opened when using the RCD while holding it.");
        await SendBui(RcdUiKey.Key, new RCDSystemMessage(RCDSettingDeconstruct));
        await CloseBui(RcdUiKey.Key);
        Assert.That(IsUiOpen(RcdUiKey.Key), Is.False, "RCD UI is still open.");

        // Deconstruct the wall.
        Assert.That(SEntMan.TryGetComponent<RCDDeconstructableComponent>(wallUid, out var wallComp), "Wall entity did not have the RCDDeconstructableComponent.");
        await Interact(wallUid, pNorth);
        AssertDeleted(FromServer(wallUid));

        // Check that the cost of the deconstruction was subtracted from the current charges.
        newCharges = sCharges.GetCurrentCharges(ToServer(rcd));
        Assert.That(initialCharges - wallComp.Cost, Is.EqualTo(newCharges), "RCD has wrong amount of charges after deconstructing something.");
        initialCharges = newCharges;

        // Deconstruct the airlock.
        Assert.That(SEntMan.TryGetComponent<RCDDeconstructableComponent>(airlockUid, out var airlockComp), "Wall entity did not have the RCDDeconstructableComponent.");
        await Interact(airlockUid, pSouth);
        AssertDeleted(FromServer(airlockUid));

        // Check that the cost of the deconstruction was subtracted from the current charges.
        newCharges = sCharges.GetCurrentCharges(ToServer(rcd));
        Assert.That(initialCharges - airlockComp.Cost, Is.EqualTo(newCharges), "RCD has wrong amount of charges after deconstructing something.");
        initialCharges = newCharges;

        // Deconstruct the steel tile.
        await Interact(null, pEast);
        await AssertTile(Plating, FromServer(pEast));

        // Check that the cost of the deconstruction was subtracted from the current charges.
        Assert.That(ProtoMan.TryIndex(RCDSettingDeconstructTile, out var settingDeconstructTile), "RCDPrototype not found.");
        newCharges = sCharges.GetCurrentCharges(ToServer(rcd));
        Assert.That(initialCharges - settingDeconstructTile.Cost, Is.EqualTo(newCharges), "RCD has wrong amount of charges after deconstructing something.");
        initialCharges = newCharges;

        // Deconstruct the plating.
        await Interact(null, pEast);
        await AssertTile(Lattice, FromServer(pEast));

        // Check that the cost of the deconstruction was subtracted from the current charges.
        newCharges = sCharges.GetCurrentCharges(ToServer(rcd));
        Assert.That(initialCharges - settingDeconstructTile.Cost, Is.EqualTo(newCharges), "RCD has wrong amount of charges after deconstructing something.");
        initialCharges = newCharges;

        // Deconstruct the lattice.
        await Interact(null, pEast);
        await AssertTile(null, FromServer(pEast));

        // Check that the cost of the deconstruction was subtracted from the current charges.
        Assert.That(ProtoMan.TryIndex(RCDSettingDeconstructLattice, out var settingDeconstructLattice), "RCDPrototype not found.");
        newCharges = sCharges.GetCurrentCharges(ToServer(rcd));
        Assert.That(initialCharges - settingDeconstructLattice.Cost, Is.EqualTo(newCharges), "RCD has wrong amount of charges after deconstructing something.");
        */
    }
}
