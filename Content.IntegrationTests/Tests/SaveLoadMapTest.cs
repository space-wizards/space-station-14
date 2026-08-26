#nullable enable
using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.CCVar;
using Robust.Shared.ContentPack;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests;

public sealed class SaveLoadMapTest : GameTest
{
    [SidedDependency(Side.Server)] private IResourceManager _sResMan = default!;
    [SidedDependency(Side.Server)] private MapLoaderSystem _sMapLoader = default!;
    [SidedDependency(Side.Server)] private SharedMapSystem _sMap = default!;
    [SidedDependency(Side.Server)] private SharedTransformSystem _sTransform = default!;

    [Test]
    [EnsureCVar(Side.Server, typeof(CCVars), nameof(CCVars.GridFill), false)]
    [RunOnSide(Side.Server)]
    [Description("Tests saving and then loading a map with multiple simple grids.")]
    public async Task SaveLoadMultiGridMap()
    {
        var mapPath = new ResPath("/Maps/Test/TestMap.yml");

        Vector2 grid1Position = new Vector2(10, 10);
        Vector2 grid2Position = new Vector2(-8, -8);

        var dir = mapPath.Directory;
        _sResMan.UserData.CreateDir(dir);

        _sMap.CreateMap(out var mapId);

        var mapGrid = _sMap.CreateGridEntity(mapId);
        _sTransform.SetWorldPosition(mapGrid, grid1Position);
        _sMap.SetTile(mapGrid, Vector2i.Zero, new Tile(typeId: 1, flags: 1, variant: 255));

        mapGrid = _sMap.CreateGridEntity(mapId);
        _sTransform.SetWorldPosition(mapGrid, grid2Position);
        _sMap.SetTile(mapGrid, Vector2i.Zero, new Tile(typeId: 2, flags: 1, variant: 254));

        Assert.That(_sMapLoader.TrySaveMap(mapId, mapPath));
        _sMap.DeleteMap(mapId);

        // Load a new map, get our new ID.
        Assert.That(_sMapLoader.TryLoadMap(mapPath, out var map, out _));
        mapId = map!.Value.Comp.MapId;

        // Try to find our first grid
        TransformComponent? gridXform = null;
        Assert.That(_sMap.TryFindGridAt(mapId, grid1Position, out var gridUid, out var mapGridComp) &&
                SEntMan.TryGetComponent(gridUid, out gridXform),
            $"Could not get the transform of the grid at ({grid1Position.X}, {grid1Position.Y})");

        Assert.Multiple(() =>
        {
            Assert.That(_sTransform.GetWorldPosition(gridXform!), Is.EqualTo(new Vector2(10, 10)));
            Assert.That(_sMap.GetTileRef(gridUid, mapGridComp!, Vector2i.Zero).Tile, Is.EqualTo(new Tile(typeId: 1, flags: 1, variant: 255)));
        });

        Assert.That(_sMap.TryFindGridAt(mapId, grid2Position, out gridUid, out mapGridComp) &&
                SEntMan.TryGetComponent(gridUid, out gridXform),
            $"Could not get the transform of the grid at ({grid2Position.X}, {grid2Position.Y})");

        Assert.Multiple(() =>
        {
            Assert.That(_sTransform.GetWorldPosition(gridXform!), Is.EqualTo(new Vector2(-8, -8)));
            Assert.That(_sMap.GetTileRef(gridUid, mapGridComp!, Vector2i.Zero).Tile, Is.EqualTo(new Tile(typeId: 2, flags: 1, variant: 254)));
        });

        _sMap.DeleteMap(mapId);
    }
}
