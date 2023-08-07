using System.Linq;
using System.Numerics;
using Content.Server.Construction.Components;
using Content.Server.Destructible;
using Content.Server.Emp;
using Content.Server.Flash;
using Content.Server.Flash.Components;
using Content.Shared.Blob;
using Content.Shared.Damage;
using Content.Shared.Destructible;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Server.Blob;

public sealed class BlobTileSystem : SharedBlobTileSystem
{
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly BlobCoreSystem _blobCoreSystem = default!;
    [Dependency] private readonly AudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly EmpSystem _empSystem = default!;


    public override void Initialize()
    {
        base.Initialize();

        // SubscribeLocalEvent<BlobTileComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<BlobTileComponent, DestructionEventArgs>(OnDestruction);
        SubscribeLocalEvent<BlobTileComponent, BlobTileGetPulseEvent>(OnPulsed);
        SubscribeLocalEvent<BlobTileComponent, GetVerbsEvent<AlternativeVerb>>(AddUpgradeVerb);
        SubscribeLocalEvent<BlobTileComponent, GetVerbsEvent<Verb>>(AddRemoveVerb);
        SubscribeLocalEvent<BlobTileComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<BlobTileComponent, FlashAttemptEvent>(OnFlashAttempt);
    }

    private void OnFlashAttempt(EntityUid uid, BlobTileComponent component, FlashAttemptEvent args)
    {
        if (args.Used == null || MetaData(args.Used.Value).EntityPrototype?.ID != "GrenadeFlashBang")
            return;
        if (component.BlobTileType == BlobTileType.Normal)
        {
            _damageableSystem.TryChangeDamage(uid, component.FlashDamage);
        }
    }

    private void OnDestruction(EntityUid uid, BlobTileComponent component, DestructionEventArgs args)
    {
        if (component.Core == null || !TryComp<BlobCoreComponent>(component.Core.Value, out var blobCoreComponent))
            return;

        var xform = Transform(uid);

        if (blobCoreComponent.CurrentChem == BlobChemType.ElectromagneticWeb)
        {
            _empSystem.EmpPulse(xform.MapPosition, 3f, 50f, 3f);
        }
    }

    private void AddRemoveVerb(EntityUid uid, BlobTileComponent component, GetVerbsEvent<Verb> args)
    {
        if (!TryComp<BlobObserverComponent>(args.User, out var ghostBlobComponent))
            return;

        if (ghostBlobComponent.Core == null ||
            !TryComp<BlobCoreComponent>(ghostBlobComponent.Core.Value, out var blobCoreComponent))
            return;

        if (ghostBlobComponent.Core.Value != component.Core)
            return;

        if (TryComp<TransformComponent>(uid, out var transformComponent) && !transformComponent.Anchored)
            return;

        if (HasComp<BlobCoreComponent>(uid))
            return;

        Verb verb = new()
        {
            Act = () => TryRemove(uid, ghostBlobComponent.Core.Value, component, blobCoreComponent),
            Text = Loc.GetString("blob-verb-remove-blob-tile"),
        };
        args.Verbs.Add(verb);
    }

    private void TryRemove(EntityUid target, EntityUid coreUid, BlobTileComponent tile, BlobCoreComponent core)
    {
        var xform = Transform(target);
        if (!_blobCoreSystem.RemoveBlobTile(target, coreUid, core))
        {
            return;
        }

        FixedPoint2 returnCost = 0;

        if (tile.ReturnCost)
        {
            switch (tile.BlobTileType)
            {
                case BlobTileType.Normal:
                {
                    returnCost = core.NormalBlobCost * core.ReturnResourceOnRemove;
                    break;
                }
                case BlobTileType.Strong:
                {
                    returnCost = core.StrongBlobCost * core.ReturnResourceOnRemove;
                    break;
                }
                case BlobTileType.Factory:
                {
                    returnCost = core.FactoryBlobCost * core.ReturnResourceOnRemove;
                    break;
                }
                case BlobTileType.Resource:
                {
                    returnCost = core.ResourceBlobCost * core.ReturnResourceOnRemove;
                    break;
                }
                case BlobTileType.Reflective:
                {
                    returnCost = core.ReflectiveBlobCost * core.ReturnResourceOnRemove;
                    break;
                }
                case BlobTileType.Node:
                {
                    returnCost = core.NodeBlobCost * core.ReturnResourceOnRemove;
                    break;
                }
            }
        }

        if (returnCost > 0)
        {
            if (TryComp<BlobCoreComponent>(tile.Core, out var blobCoreComponent) && blobCoreComponent.Observer != null)
            {
                _popup.PopupCoordinates(Loc.GetString("blob-get-resource", ("point", returnCost)),
                    xform.Coordinates,
                    blobCoreComponent.Observer.Value,
                    PopupType.LargeGreen);
            }
            _blobCoreSystem.ChangeBlobPoint(coreUid, returnCost, core);
        }
    }

    private void OnGetState(EntityUid uid, BlobTileComponent component, ref ComponentGetState args)
    {
        args.State = new BlobTileComponentState()
        {
            Color = component.Color
        };
    }

    private void OnPulsed(EntityUid uid, BlobTileComponent component, BlobTileGetPulseEvent args)
    {

        if (!TryComp<BlobTileComponent>(uid, out var blobTileComponent) || blobTileComponent.Core == null ||
            !TryComp<BlobCoreComponent>(blobTileComponent.Core.Value, out var blobCoreComponent))
            return;

        if (blobCoreComponent.CurrentChem == BlobChemType.RegenerativeMateria)
        {
            var healCore = new DamageSpecifier();
            foreach (var keyValuePair in component.HealthOfPulse.DamageDict)
            {
                healCore.DamageDict.Add(keyValuePair.Key, keyValuePair.Value * 10);
            }
            _damageableSystem.TryChangeDamage(uid, healCore);
        }
        else
        {
            _damageableSystem.TryChangeDamage(uid, component.HealthOfPulse);
        }

        if (!args.Explain)
            return;

        var xform = Transform(uid);

        if (!_map.TryGetGrid(xform.GridUid, out var grid))
        {
            return;
        }

        var mobTile = grid.GetTileRef(xform.Coordinates);

        var mobAdjacentTiles = new[]
        {
            mobTile.GridIndices.Offset(Direction.East),
            mobTile.GridIndices.Offset(Direction.West),
            mobTile.GridIndices.Offset(Direction.North),
            mobTile.GridIndices.Offset(Direction.South)
        };

        var localPos = xform.Coordinates.Position;

        var radius = 1.0f;

        var innerTiles = grid.GetLocalTilesIntersecting(
            new Box2(localPos + new Vector2(-radius, -radius), localPos + new Vector2(radius, radius))).ToArray();

        foreach (var innerTile in innerTiles)
        {
            if (!mobAdjacentTiles.Contains(innerTile.GridIndices))
            {
                continue;
            }

            foreach (var ent in grid.GetAnchoredEntities(innerTile.GridIndices))
            {
                if (!HasComp<DestructibleComponent>(ent) || !HasComp<ConstructionComponent>(ent))
                    continue;
                _damageableSystem.TryChangeDamage(ent, blobCoreComponent.ChemDamageDict[blobCoreComponent.CurrentChem]);
                _audioSystem.PlayPvs(blobCoreComponent.AttackSound, uid, AudioParams.Default);
                args.Explain = true;
                return;
            }
            var spawn = true;
            foreach (var ent in grid.GetAnchoredEntities(innerTile.GridIndices))
            {
                if (!HasComp<BlobTileComponent>(ent))
                    continue;
                spawn = false;
                break;
            }

            if (!spawn)
                continue;

            var location = innerTile.GridIndices.ToEntityCoordinates(xform.GridUid.Value, _map);

            if (_blobCoreSystem.TransformBlobTile(null,
                    blobTileComponent.Core.Value,
                    blobCoreComponent.NormalBlobTile,
                    location,
                    blobCoreComponent,
                    false))
                return;
        }
    }

    private void AddUpgradeVerb(EntityUid uid, BlobTileComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!TryComp<BlobObserverComponent>(args.User, out var ghostBlobComponent))
            return;

        if (ghostBlobComponent.Core == null ||
            !TryComp<BlobCoreComponent>(ghostBlobComponent.Core.Value, out var blobCoreComponent))
            return;

        if (TryComp<TransformComponent>(uid, out var transformComponent) && !transformComponent.Anchored)
            return;

        var verbName = component.BlobTileType switch
        {
            BlobTileType.Normal => Loc.GetString("blob-verb-upgrade-to-strong"),
            BlobTileType.Strong => Loc.GetString("blob-verb-upgrade-to-reflective"),
            _ => "Upgrade"
        };

        AlternativeVerb verb = new()
        {
            Act = () => TryUpgrade(uid, args.User, ghostBlobComponent.Core.Value, component, blobCoreComponent),
            Text = verbName
        };
        args.Verbs.Add(verb);
    }

    private void TryUpgrade(EntityUid target, EntityUid user, EntityUid coreUid, BlobTileComponent tile, BlobCoreComponent core)
    {
        var xform = Transform(target);
        if (tile.BlobTileType == BlobTileType.Normal)
        {
            if (!_blobCoreSystem.TryUseAbility(user, coreUid, core, core.StrongBlobCost))
                return;

            _blobCoreSystem.TransformBlobTile(target,
                coreUid,
                core.StrongBlobTile,
                xform.Coordinates,
                core,
                transformCost: core.StrongBlobCost);
        }
        else if (tile.BlobTileType == BlobTileType.Strong)
        {
            if (!_blobCoreSystem.TryUseAbility(user, coreUid, core, core.ReflectiveBlobCost))
                return;

            _blobCoreSystem.TransformBlobTile(target,
                coreUid,
                core.ReflectiveBlobTile,
                xform.Coordinates,
                core,
                transformCost: core.ReflectiveBlobCost);
        }
    }

    /* This work very bad.
     I replace invisible
     wall to teleportation observer
     if he moving away from blob tile */

    // private void OnStartup(EntityUid uid, BlobCellComponent component, ComponentStartup args)
    // {
    //     var xform = Transform(uid);
    //     var radius = 2.5f;
    //     var wallSpacing = 1.5f; // Расстояние между стенами и центральной областью
    //
    //     if (!_map.TryGetGrid(xform.GridUid, out var grid))
    //     {
    //         return;
    //     }
    //
    //     var localpos = xform.Coordinates.Position;
    //
    //     // Получаем тайлы в области с радиусом 2.5
    //     var allTiles = grid.GetLocalTilesIntersecting(
    //         new Box2(localpos + new Vector2(-radius, -radius), localpos + new Vector2(radius, radius))).ToArray();
    //
    //     // Получаем тайлы в области с радиусом 1.5
    //     var innerTiles = grid.GetLocalTilesIntersecting(
    //         new Box2(localpos + new Vector2(-wallSpacing, -wallSpacing), localpos + new Vector2(wallSpacing, wallSpacing))).ToArray();
    //
    //     foreach (var tileref in innerTiles)
    //     {
    //         foreach (var ent in grid.GetAnchoredEntities(tileref.GridIndices))
    //         {
    //             if (HasComp<BlobBorderComponent>(ent))
    //                 QueueDel(ent);
    //             if (HasComp<BlobCellComponent>(ent))
    //             {
    //                 var blockTiles = grid.GetLocalTilesIntersecting(
    //                     new Box2(Transform(ent).Coordinates.Position + new Vector2(-wallSpacing, -wallSpacing),
    //                         Transform(ent).Coordinates.Position + new Vector2(wallSpacing, wallSpacing))).ToArray();
    //                 allTiles = allTiles.Except(blockTiles).ToArray();
    //             }
    //         }
    //     }
    //
    //     var outerTiles = allTiles.Except(innerTiles).ToArray();
    //
    //     foreach (var tileRef in outerTiles)
    //     {
    //         foreach (var ent in grid.GetAnchoredEntities(tileRef.GridIndices))
    //         {
    //             if (HasComp<BlobCellComponent>(ent))
    //             {
    //                 var blockTiles = grid.GetLocalTilesIntersecting(
    //                     new Box2(Transform(ent).Coordinates.Position + new Vector2(-wallSpacing, -wallSpacing),
    //                         Transform(ent).Coordinates.Position + new Vector2(wallSpacing, wallSpacing))).ToArray();
    //                 outerTiles = outerTiles.Except(blockTiles).ToArray();
    //             }
    //         }
    //     }
    //
    //     foreach (var tileRef in outerTiles)
    //     {
    //         var spawn = true;
    //         foreach (var ent in grid.GetAnchoredEntities(tileRef.GridIndices))
    //         {
    //             if (HasComp<BlobBorderComponent>(ent))
    //             {
    //                 spawn = false;
    //                 break;
    //             }
    //         }
    //         if (spawn)
    //             EntityManager.SpawnEntity("BlobBorder", tileRef.GridIndices.ToEntityCoordinates(xform.GridUid.Value, _map));
    //     }
    // }

    // private void OnDestruction(EntityUid uid, BlobTileComponent component, DestructionEventArgs args)
    // {
    //     var xform = Transform(uid);
    //     var radius = 1.0f;
    //
    //     if (!_map.TryGetGrid(xform.GridUid, out var grid))
    //     {
    //         return;
    //     }
    //
    //     var localPos = xform.Coordinates.Position;
    //
    //     var innerTiles = grid.GetLocalTilesIntersecting(
    //         new Box2(localPos + new Vector2(-radius, -radius), localPos + new Vector2(radius, radius)), false).ToArray();
    //
    //     var centerTile = grid.GetLocalTilesIntersecting(
    //         new Box2(localPos, localPos)).ToArray();
    //
    //     innerTiles = innerTiles.Except(centerTile).ToArray();
    //
    //     foreach (var tileref in innerTiles)
    //     {
    //         foreach (var ent in grid.GetAnchoredEntities(tileref.GridIndices))
    //         {
    //             if (!HasComp<BlobTileComponent>(ent))
    //                 continue;
    //             var blockTiles = grid.GetLocalTilesIntersecting(
    //                 new Box2(Transform(ent).Coordinates.Position + new Vector2(-radius, -radius),
    //                     Transform(ent).Coordinates.Position + new Vector2(radius, radius)), false).ToArray();
    //
    //             var tilesToRemove = new List<TileRef>();
    //
    //             foreach (var blockTile in blockTiles)
    //             {
    //                 tilesToRemove.Add(blockTile);
    //             }
    //
    //             innerTiles = innerTiles.Except(tilesToRemove).ToArray();
    //         }
    //     }
    //
    //     foreach (var tileRef in innerTiles)
    //     {
    //         foreach (var ent in grid.GetAnchoredEntities(tileRef.GridIndices))
    //         {
    //             if (HasComp<BlobBorderComponent>(ent))
    //             {
    //                 QueueDel(ent);
    //             }
    //         }
    //     }
    //
    //     EntityManager.SpawnEntity(component.BlobBorder, xform.Coordinates);
    // }
    //
    // private void OnStartup(EntityUid uid, BlobTileComponent component, ComponentStartup args)
    // {
    //     var xform = Transform(uid);
    //     var wallSpacing = 1.0f;
    //
    //     if (!_map.TryGetGrid(xform.GridUid, out var grid))
    //     {
    //         return;
    //     }
    //
    //     var localPos = xform.Coordinates.Position;
    //
    //     var innerTiles = grid.GetLocalTilesIntersecting(
    //         new Box2(localPos + new Vector2(-wallSpacing, -wallSpacing), localPos + new Vector2(wallSpacing, wallSpacing)), false).ToArray();
    //
    //     var centerTile = grid.GetLocalTilesIntersecting(
    //         new Box2(localPos, localPos)).ToArray();
    //
    //     foreach (var tileRef in centerTile)
    //     {
    //         foreach (var ent in grid.GetAnchoredEntities(tileRef.GridIndices))
    //         {
    //             if (HasComp<BlobBorderComponent>(ent))
    //                 QueueDel(ent);
    //         }
    //     }
    //     innerTiles = innerTiles.Except(centerTile).ToArray();
    //
    //     foreach (var tileref in innerTiles)
    //     {
    //         var spaceNear = false;
    //         var hasBlobTile = false;
    //         foreach (var ent in grid.GetAnchoredEntities(tileref.GridIndices))
    //         {
    //             if (!HasComp<BlobTileComponent>(ent))
    //                 continue;
    //             var blockTiles = grid.GetLocalTilesIntersecting(
    //                 new Box2(Transform(ent).Coordinates.Position + new Vector2(-wallSpacing, -wallSpacing),
    //                     Transform(ent).Coordinates.Position + new Vector2(wallSpacing, wallSpacing)), false).ToArray();
    //
    //             var tilesToRemove = new List<TileRef>();
    //
    //             foreach (var blockTile in blockTiles)
    //             {
    //                 if (blockTile.Tile.IsEmpty)
    //                 {
    //                     spaceNear = true;
    //                 }
    //                 else
    //                 {
    //                     tilesToRemove.Add(blockTile);
    //                 }
    //             }
    //
    //             innerTiles = innerTiles.Except(tilesToRemove).ToArray();
    //
    //             hasBlobTile = true;
    //         }
    //
    //         if (!hasBlobTile || spaceNear)
    //             continue;
    //         {
    //             foreach (var ent in grid.GetAnchoredEntities(tileref.GridIndices))
    //             {
    //                 if (HasComp<BlobBorderComponent>(ent))
    //                 {
    //                     QueueDel(ent);
    //                 }
    //             }
    //         }
    //     }
    //
    //     var spaceNearCenter = false;
    //
    //     foreach (var tileRef in innerTiles)
    //     {
    //         var spawn = true;
    //         if (tileRef.Tile.IsEmpty)
    //         {
    //             spaceNearCenter = true;
    //             spawn = false;
    //         }
    //         if (grid.GetAnchoredEntities(tileRef.GridIndices).Any(ent => HasComp<BlobBorderComponent>(ent)))
    //         {
    //             spawn = false;
    //         }
    //         if (spawn)
    //             EntityManager.SpawnEntity(component.BlobBorder, tileRef.GridIndices.ToEntityCoordinates(xform.GridUid.Value, _map));
    //     }
    //     if (spaceNearCenter)
    //     {
    //         EntityManager.SpawnEntity(component.BlobBorder, xform.Coordinates);
    //     }
    // }
}
