using System.Collections.Generic;
using Content.IntegrationTests.Fixtures;
using Content.Server.Radiation.Components;
using Content.Server.Radiation.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests.Radiation;

[TestFixture]
[TestOf(typeof(RadiationSystem))]
public sealed class RadiationTest : GameTest
{
    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  id: RadiationTestSource
  components:
  - type: RadiationSource
    intensity: 10
    slope: 1

- type: entity
  id: RadiationTestReceiver
  components:
  - type: RadiationReceiver

- type: entity
  id: RadiationTestBlocker
  components:
  - type: RadiationBlocker
    resistance: 5
";

    [Test]
    public async Task SourceIrradiatesReceiver()
    {
        var pair = Pair;
        var server = pair.Server;
        var testMap = await pair.CreateTestMap();

        EntityUid receiver = default;

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;

            entMan.SpawnAtPosition("RadiationTestSource", new EntityCoordinates(testMap.Grid, 0.5f, 0.5f));
            receiver = entMan.SpawnAtPosition("RadiationTestReceiver", new EntityCoordinates(testMap.Grid, 2.5f, 0.5f));

            entMan.System<RadiationSystem>().Update(2f);
        });

        await server.WaitAssertion(() =>
        {
            var receiverComp = server.EntMan.GetComponent<RadiationReceiverComponent>(receiver);
            Assert.That(receiverComp.CurrentRadiation, Is.EqualTo(8f).Within(0.01f));
        });
    }

    [Test]
    public async Task DisablingSourceClearsRadiation()
    {
        var pair = Pair;
        var server = pair.Server;
        var testMap = await pair.CreateTestMap();

        EntityUid source = default;
        EntityUid receiver = default;

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;

            source = entMan.SpawnAtPosition("RadiationTestSource", new EntityCoordinates(testMap.Grid, 0.5f, 0.5f));
            receiver = entMan.SpawnAtPosition("RadiationTestReceiver", new EntityCoordinates(testMap.Grid, 2.5f, 0.5f));

            entMan.System<RadiationSystem>().Update(2f);
        });

        await server.WaitAssertion(() =>
        {
            var receiverComp = server.EntMan.GetComponent<RadiationReceiverComponent>(receiver);
            Assert.That(receiverComp.CurrentRadiation, Is.EqualTo(8f).Within(0.01f));

            server.EntMan.System<RadiationSystem>().SetSourceEnabled(source, false);
            server.EntMan.System<RadiationSystem>().Update(2f);
        });

        await server.WaitAssertion(() =>
        {
            var receiverComp = server.EntMan.GetComponent<RadiationReceiverComponent>(receiver);
            Assert.That(receiverComp.CurrentRadiation, Is.Zero);
        });
    }

    [Test]
    public async Task BlockerAbsorbsRadiation()
    {
        var pair = Pair;
        var server = pair.Server;
        var testMap = await pair.CreateTestMap();

        EntityUid receiver = default;
        EntityUid blocker = default;

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            var transform = entMan.System<SharedTransformSystem>();
            var mapSys = entMan.System<SharedMapSystem>();

            for (var x = 1; x <= 4; x++)
                mapSys.SetTile(testMap.Grid.Owner, testMap.Grid.Comp, new Vector2i(x, 0), testMap.Tile.Tile);

            entMan.SpawnAtPosition("RadiationTestSource", new EntityCoordinates(testMap.Grid, 0.5f, 0.5f));
            receiver = entMan.SpawnAtPosition("RadiationTestReceiver", new EntityCoordinates(testMap.Grid, 4.5f, 0.5f));
            blocker = entMan.SpawnAtPosition("RadiationTestBlocker", new EntityCoordinates(testMap.Grid, 2.5f, 0.5f));

            transform.AnchorEntity(blocker, entMan.GetComponent<TransformComponent>(blocker));

            entMan.System<RadiationSystem>().Update(2f);
        });

        await server.WaitAssertion(() =>
        {
            var receiverComp = server.EntMan.GetComponent<RadiationReceiverComponent>(receiver);
            Assert.That(receiverComp.CurrentRadiation, Is.EqualTo(1f).Within(0.01f));
        });

        await server.WaitAssertion(() =>
        {
            server.EntMan.DeleteEntity(blocker);
            server.EntMan.System<RadiationSystem>().Update(2f);
        });

        await server.WaitAssertion(() =>
        {
            var receiverComp = server.EntMan.GetComponent<RadiationReceiverComponent>(receiver);
            Assert.That(receiverComp.CurrentRadiation, Is.EqualTo(6f).Within(0.01f));
        });
    }

    [Test]
    public async Task ManySourcesAndReceivers()
    {
        var pair = Pair;
        var server = pair.Server;
        var testMap = await pair.CreateTestMap();

        var receivers = new List<EntityUid>();

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            var mapSys = entMan.System<SharedMapSystem>();

            for (var x = 0; x < 10; x++)
            {
                for (var y = 0; y < 6; y++)
                    mapSys.SetTile(testMap.Grid.Owner, testMap.Grid.Comp, new Vector2i(x, y), testMap.Tile.Tile);
            }

            for (var x = 0; x < 5; x++)
            {
                for (var y = 0; y < 5; y++)
                    entMan.SpawnAtPosition("RadiationTestSource", new EntityCoordinates(testMap.Grid, x + 0.5f, y + 0.5f));
            }

            for (var x = 5; x < 10; x++)
            {
                for (var y = 0; y < 5; y++)
                    receivers.Add(entMan.SpawnAtPosition("RadiationTestReceiver", new EntityCoordinates(testMap.Grid, x + 0.5f, y + 0.5f)));
            }

            entMan.System<RadiationSystem>().Update(2f);
        });

        await server.WaitAssertion(() =>
        {
            foreach (var receiver in receivers)
            {
                var receiverComp = server.EntMan.GetComponent<RadiationReceiverComponent>(receiver);
                Assert.That(receiverComp.CurrentRadiation, Is.GreaterThan(0f));
            }
        });
    }
}
