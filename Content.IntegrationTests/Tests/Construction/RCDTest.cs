using System.Numerics;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Charges.Systems;
using Content.Shared.RCD;
using Content.Shared.RCD.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
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
        var pNorth = new EntityCoordinates(SPlayer, new Vector2(0, 1));
        var pSouth = new EntityCoordinates(SPlayer, new Vector2(0, -1));
        var pEast = new EntityCoordinates(SPlayer, new Vector2(1, 0));
        var pWest = new EntityCoordinates(SPlayer, new Vector2(-1, 0));

        // Use EntityCoordinates relative to the grid because the player turns around when interacting.
        pNorth = Transform.WithEntityId(pNorth, MapData.Grid);
        pSouth = Transform.WithEntityId(pSouth, MapData.Grid);
        pEast = Transform.WithEntityId(pEast, MapData.Grid);
        pWest = Transform.WithEntityId(pWest, MapData.Grid);

        await SetTile(Plating, SEntMan.GetNetCoordinates(pNorth), MapData.Grid);
        await SetTile(Plating, SEntMan.GetNetCoordinates(pSouth), MapData.Grid);
        await SetTile(Plating, SEntMan.GetNetCoordinates(pEast), MapData.Grid);
        await SetTile(Lattice, SEntMan.GetNetCoordinates(pWest), MapData.Grid);

        Assert.That(ProtoMan.TryIndex(RCDSettingWall, out var settingWall), $"RCDPrototype not found: {RCDSettingWall}.");
        Assert.That(settingWall.Prototype, Is.Not.Null, "RCDPrototype has a null spawning prototype.");
        Assert.That(ProtoMan.TryIndex(RCDSettingAirlock, out var settingAirlock), $"RCDPrototype not found: {RCDSettingAirlock}.");
        Assert.That(settingAirlock.Prototype, Is.Not.Null, $"RCDPrototype has a null spawning prototype.");
        Assert.That(ProtoMan.TryIndex(RCDSettingPlating, out var settingPlating), $"RCDPrototype not found: {RCDSettingPlating}.");
        Assert.That(settingPlating.Prototype, Is.Not.Null, "RCDPrototype has a null spawning prototype.");
        Assert.That(ProtoMan.TryIndex(RCDSettingFloorSteel, out var settingFloorSteel), $"RCDPrototype not found: {RCDSettingFloorSteel}.");
        Assert.That(settingFloorSteel.Prototype, Is.Not.Null, "RCDPrototype has a null spawning prototype.");
        Assert.That(ProtoMan.TryIndex(RCDSettingDeconstructTile, out var settingDeconstructTile), $"RCDPrototype not found: {RCDSettingDeconstructTile}.");
        Assert.That(ProtoMan.TryIndex(RCDSettingDeconstructLattice, out var settingDeconstructLattice), $"RCDPrototype not found: {RCDSettingDeconstructLattice}.");

        var rcd = await PlaceInHands(RCDProtoId);

        // Give the RCD enough charges to do everything.
        var sCharges = SEntMan.System<SharedChargesSystem>();
        await Server.WaitPost(() =>
        {
            sCharges.SetMaxCharges(ToServer(rcd), 10000);
            sCharges.SetCharges(ToServer(rcd), 10000);
        });
        var initialCharges = sCharges.GetCurrentCharges(ToServer(rcd));
        Assert.That(initialCharges, Is.EqualTo(10000), "RCD did not have the correct amount of charges.");

        // Make sure that picking it up did not open the UI.
        Assert.That(IsUiOpen(RcdUiKey.Key), Is.False, "RCD UI was opened when picking it up.");

        // Switch to building walls.
        await SetRcdProto(rcd, RCDSettingWall);

        // Build a wall next to the player.
        await Interact(null, pNorth);

        // Check that there is exactly one wall.
        await RunSeconds(settingWall.Delay + 1); // wait for the construction to finish
        await AssertEntityLookup((settingWall.Prototype, 1));

        // Check that the wall is in the correct tile.
        var wallUid = await FindEntity(settingWall.Prototype);
        var wallNetUid = FromServer(wallUid);
        AssertLocation(wallNetUid, FromServer(pNorth));

        // Check that the cost of the wall was subtracted from the current charges.
        var newCharges = sCharges.GetCurrentCharges(ToServer(rcd));
        Assert.That(initialCharges - settingWall.Cost, Is.EqualTo(newCharges), "RCD has wrong amount of charges after building something.");
        initialCharges = newCharges;

        // Try building another wall in the same spot.
        await Interact(null, pNorth);
        await RunSeconds(settingWall.Delay + 1); // wait for the construction to finish

        // Check that there is still exactly one wall.
        await AssertEntityLookup((settingWall.Prototype, 1));

        // Check that the failed construction did not cost us any charges.
        newCharges = sCharges.GetCurrentCharges(ToServer(rcd));
        Assert.That(initialCharges, Is.EqualTo(newCharges), "RCD has wrong amount of charges after failing to build something.");

        // Switch to building airlocks.
        await SetRcdProto(rcd, RCDSettingAirlock);

        // Build an airlock next to the player.
        await Interact(null, pSouth);

        // Check that there is exactly one airlock.
        await RunSeconds(settingAirlock.Delay + 1); // wait for the construction to finish
        await AssertEntityLookup(
            (settingWall.Prototype, 1),
            (settingAirlock.Prototype, 1)
            );

        // Check that the airlock is in the correct tile.
        var airlockUid = await FindEntity(settingAirlock.Prototype);
        var airlockNetUid = FromServer(airlockUid);
        AssertLocation(airlockNetUid, FromServer(pSouth));

        // Check that the cost of the airlock was subtracted from the current charges.
        newCharges = sCharges.GetCurrentCharges(ToServer(rcd));
        Assert.That(initialCharges - settingAirlock.Cost, Is.EqualTo(newCharges), "RCD has wrong amount of charges after building something.");
        initialCharges = newCharges;

        // Switch to building plating.
        await SetRcdProto(rcd, RCDSettingPlating);

        // Try building plating on existing plating.
        await AssertTile(settingPlating.Prototype, FromServer(pEast));
        await Interact(null, pEast);

        // Check that the tile did not change.
        await AssertTile(settingPlating.Prototype, FromServer(pEast));

        // Check that the failed construction did not cost us any charges.
        await RunSeconds(settingPlating.Delay + 1); // wait for the construction to finish
        newCharges = sCharges.GetCurrentCharges(ToServer(rcd));
        Assert.That(initialCharges, Is.EqualTo(newCharges), "RCD has wrong amount of charges after failing to build something.");

        // Try building plating on top of lattice.
        await AssertTile(Lattice, FromServer(pWest));
        await Interact(null, pWest);
        await RunSeconds(settingPlating.Delay + 1); // wait for the construction to finish

        // Check that the tile is now plating.
        await AssertTile(settingPlating.Prototype, FromServer(pWest));

        // Check that the cost of the plating was subtracted from the current charges.
        newCharges = sCharges.GetCurrentCharges(ToServer(rcd));
        Assert.That(initialCharges - settingPlating.Cost, Is.EqualTo(newCharges), "RCD has wrong amount of charges after building something.");
        initialCharges = newCharges;

        // Switch to building steel tiles.
        await SetRcdProto(rcd, RCDSettingFloorSteel);

        // Try building a steel tile on top of plating.
        await Interact(null, pEast);

        // Check that the tile is now a steel tile.
        await RunSeconds(settingFloorSteel.Delay + 1); // wait for the construction to finish
        await AssertTile(settingFloorSteel.Prototype, FromServer(pEast));

        // Check that the cost of the plating was subtracted from the current charges.
        newCharges = sCharges.GetCurrentCharges(ToServer(rcd));
        Assert.That(initialCharges - settingFloorSteel.Cost, Is.EqualTo(newCharges), "RCD has wrong amount of charges after building something.");
        initialCharges = newCharges;

        // Switch to deconstruction mode.
        await SetRcdProto(rcd, RCDSettingDeconstruct);

        // Deconstruct the wall.
        Assert.That(SEntMan.TryGetComponent<RCDDeconstructableComponent>(wallUid, out var wallComp), "Wall entity did not have the RCDDeconstructableComponent.");
        await Interact(wallUid, pNorth);
        await RunSeconds(wallComp.Delay + 1); // wait for the deconstruction to finish
        AssertDeleted(wallNetUid);

        // Check that the cost of the deconstruction was subtracted from the current charges.
        newCharges = sCharges.GetCurrentCharges(ToServer(rcd));
        Assert.That(initialCharges - wallComp.Cost, Is.EqualTo(newCharges), "RCD has wrong amount of charges after deconstructing something.");
        initialCharges = newCharges;

        // Deconstruct the airlock.
        Assert.That(SEntMan.TryGetComponent<RCDDeconstructableComponent>(airlockUid, out var airlockComp), "Wall entity did not have the RCDDeconstructableComponent.");
        await Interact(airlockUid, pSouth);
        await RunSeconds(airlockComp.Delay + 1); // wait for the deconstruction to finish
        AssertDeleted(airlockNetUid);

        // Check that the cost of the deconstruction was subtracted from the current charges.
        newCharges = sCharges.GetCurrentCharges(ToServer(rcd));
        Assert.That(initialCharges - airlockComp.Cost, Is.EqualTo(newCharges), "RCD has wrong amount of charges after deconstructing something.");
        initialCharges = newCharges;

        // Deconstruct the steel tile.
        await Interact(null, pEast);
        await RunSeconds(settingDeconstructTile.Delay + 1); // wait for the deconstruction to finish
        await AssertTile(Lattice, FromServer(pEast));

        // Check that the cost of the deconstruction was subtracted from the current charges.
        newCharges = sCharges.GetCurrentCharges(ToServer(rcd));
        Assert.That(initialCharges - settingDeconstructTile.Cost, Is.EqualTo(newCharges), "RCD has wrong amount of charges after deconstructing something.");
        initialCharges = newCharges;

        // Deconstruct the plating.
        await Interact(null, pWest);
        await RunSeconds(settingDeconstructTile.Delay + 1); // wait for the deconstruction to finish
        await AssertTile(Lattice, FromServer(pWest));

        // Check that the cost of the deconstruction was subtracted from the current charges.
        newCharges = sCharges.GetCurrentCharges(ToServer(rcd));
        Assert.That(initialCharges - settingDeconstructTile.Cost, Is.EqualTo(newCharges), "RCD has wrong amount of charges after deconstructing something.");
        initialCharges = newCharges;

        // Deconstruct the lattice.
        await Interact(null, pWest);
        await RunSeconds(settingDeconstructLattice.Delay + 1); // wait for the deconstruction to finish
        await AssertTile(null, FromServer(pWest));

        // Check that the cost of the deconstruction was subtracted from the current charges.
        newCharges = sCharges.GetCurrentCharges(ToServer(rcd));
        Assert.That(initialCharges - settingDeconstructLattice.Cost, Is.EqualTo(newCharges), "RCD has wrong amount of charges after deconstructing something.");

        // Wait for the visual effect to disappear.
        await RunSeconds(3);

        // Check that there are no entities left.
        await AssertEntityLookup();
    }

    private async Task SetRcdProto(NetEntity rcd, ProtoId<RCDPrototype> protoId)
    {
        await UseInHand();
        await RunTicks(3);
        Assert.That(IsUiOpen(RcdUiKey.Key), Is.True, "RCD UI was not opened when using the RCD while holding it.");

        // Simulating a click on the right control for nested radial menus is very complicated.
        // So we just manually send a networking message from the client to tell the server we selected an option.
        // TODO: Write a separate test for clicking through a SimpleRadialMenu.
        await SendBui(RcdUiKey.Key, new RCDSystemMessage(protoId), rcd);
        await CloseBui(RcdUiKey.Key, rcd);
        Assert.That(IsUiOpen(RcdUiKey.Key), Is.False, "RCD UI is still open.");
    }
}
