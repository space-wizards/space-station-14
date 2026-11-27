using System.Collections.Generic;
using Content.Client.IconSmoothing;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Utility;
using Content.Shared.Fluids.Components;
using Content.Shared.Maps;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.IconSmooth;

[TestFixture]
public sealed class IconSmoothTest : GameTest
{
    public override PoolSettings PoolSettings => new()
    {
        Dirty = true,
        Connected = true,
    };

    private static readonly ProtoId<ContentTileDefinition> TileRef = "Plating";

    [SidedDependency(Side.Server)] private ITileDefinitionManager _tileDefMan = default!;
    [SidedDependency(Side.Client)] private IconSmoothSystem _iconSmooth = default!;
    [SidedDependency(Side.Server)] private SharedMapSystem _map = default!;
    [SidedDependency(Side.Server)] private SharedTransformSystem _xform = default!;

    private static readonly string[] IconSmoothEntities = GameDataScrounger.EntitiesWithComponent("IconSmooth");
    private List<EntityUid> _spawned = new ();

    [Test]
    public async Task PreventIconSmoothCacheLeaks()
    {
        await Pair.CreateTestMap();
        await Pair.SyncTicks();

        var dimensions = (int)Math.Sqrt(IconSmoothEntities.Length) + 1;

        // Set up a thin line of tiles to place our objects on. They should be anchored for a "realistic" scenario...
        await Server.WaitPost(() =>
        {
            for (var x = 0; x < dimensions; x++)
            {
                for (var y = 0; y < dimensions; y++)
                {
                    _map.SetTile(TestMap!.Grid,
                        TestMap!.Grid,
                        new Vector2i(x, y),
                        new Tile(_tileDefMan[TileRef].TileId));
                }
            }
        });

        // Spawn entities
        await Server.WaitPost(() =>
        {
            PlaceEntities(dimensions);
        });
        await Pair.RunUntilSynced();

        var cache = CComp<IconSmoothGridComponent>(TestMap!.CGridUid);

        // Ensure client is aware of the entities and has cached them.
        await Client.WaitAssertion(() =>
        {
            ValidateCache(cache, _spawned.Count);
        });

        // Disable and Re-Enable IconSmooth on all entities.
        await Client.WaitPost(() =>
        {
            SetEnabled(false);
        });
        await Client.WaitAssertion(() =>
        {
            ValidateCache(cache, 0);
        });
        await Client.WaitPost(() =>
        {
            SetEnabled(true);
        });
        await Client.WaitAssertion(() =>
        {
            ValidateCache(cache, _spawned.Count);
        });

        // Anchor and then unanchor all entities.
        await Server.WaitPost(UnanchorAll);
        await Pair.RunUntilSynced();
        await Client.WaitAssertion(() =>
        {
            ValidateCache(cache, 0);
        });
        await Server.WaitPost(AnchorAll);
        await Pair.RunUntilSynced();
        await Client.WaitAssertion(() =>
        {
            ValidateCache(cache, _spawned.Count);
        });

        // Delete and then respawn all entities.
        await Server.WaitPost(DeleteAll);
        await Pair.RunUntilSynced();
        await Client.WaitAssertion(() =>
        {
            ValidateCache(cache, 0);
        });
        await Server.WaitPost(() =>
        {
            PlaceEntities(dimensions);
        });
        await Pair.RunUntilSynced();
        await Client.WaitAssertion(() =>
        {
            ValidateCache(cache, _spawned.Count);
        });

        // Delete the grid.
        await Server.WaitPost(() =>
        {
            SEntMan.DeleteEntity(TestMap.Grid);
        });
        await Pair.RunUntilSynced();
        Assert.That(_iconSmooth.CountTracked(), Is.EqualTo(0));
    }

    private void ValidateCache(IconSmoothGridComponent cache, int count)
    {
        var tracked = _iconSmooth.CountTracked();
        Assert.That(tracked, Is.EqualTo(count), $"Cached: {tracked}, Count: {count}");

        var cached = 0;
        foreach (var (_, chunk) in cache.Chunks)
        {
            foreach (var tile in chunk.Tiles)
            {
                if (tile == null)
                    continue;

                cached++;
            }
        }

        Assert.That(cached, Is.EqualTo(count), $"Cached: {cached}, Count: {count}");
    }

    private void UnanchorAll()
    {
        foreach (var uid in _spawned)
        {
            _xform.Unanchor(uid);
        }
    }

    private void AnchorAll()
    {
        foreach (var uid in _spawned)
        {
            _xform.AnchorEntity(uid);
        }
    }

    private void SetEnabled(bool enabled)
    {
        var iconEnumerator = CEntMan.EntityQueryEnumerator<IconSmoothComponent>();
        while (iconEnumerator.MoveNext(out var uid, out var comp))
        {
            _iconSmooth.SetEnabled((uid, comp), enabled);
        }
    }

    private void PlaceEntities(int dimensions)
    {
        var i = 0;
        for (var x = 0; x < dimensions; x++)
        {
            for (var y = 0; y < dimensions; y++)
            {
                if (i >= IconSmoothEntities.Length)
                    return;

                var proto = IconSmoothEntities[i++];
                // Puddles will disable icon smooth and delete themselves if empty or un-anchored so they will fuck up the test.
                if (!SProtoMan.Resolve(proto, out var resolved) || resolved.HasComp<PuddleComponent>(SEntMan.ComponentFactory))
                    continue;

                _spawned.Add(SEntMan.SpawnEntity(proto, new EntityCoordinates(TestMap!.Grid, x + 0.5f, y + 0.5f)));
            }
        }
    }

    private void DeleteAll()
    {
        foreach (var uid in _spawned)
        {
            SEntMan.DeleteEntity(uid);
        }
        _spawned.Clear();
    }
}
