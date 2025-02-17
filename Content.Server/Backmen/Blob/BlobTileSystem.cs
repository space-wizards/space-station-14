using System.Linq;
using System.Numerics;
using Content.Server.Construction.Components;
using Content.Server.Destructible;
using Content.Server.Emp;
using Content.Server.Flash;
using Content.Shared.Backmen.Blob;
using Content.Shared.Backmen.Blob.Components;
using Content.Shared.Damage;
using Content.Shared.Destructible;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Map.Components;

namespace Content.Server.Backmen.Blob;

public sealed class BlobTileSystem : SharedBlobTileSystem
{
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly BlobCoreSystem _blobCoreSystem = default!;
    [Dependency] private readonly AudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly EmpSystem _empSystem = default!;
    [Dependency] private readonly MapSystem _mapSystem = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    private EntityQuery<BlobCoreComponent> _blobCoreQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlobTileComponent, DestructionEventArgs>(OnDestruction);
        SubscribeLocalEvent<BlobTileComponent, BlobTileGetPulseEvent>(OnPulsed);
        SubscribeLocalEvent<BlobTileComponent, FlashAttemptEvent>(OnFlashAttempt);
        SubscribeLocalEvent<BlobTileComponent, EntityTerminatingEvent>(OnTerminate);

        _blobCoreQuery = GetEntityQuery<BlobCoreComponent>();
    }

    private void OnTerminate(EntityUid uid, BlobTileComponent component, EntityTerminatingEvent args)
    {
        if(component.Core == null || TerminatingOrDeleted(component.Core.Value) || !_blobCoreQuery.TryComp(component.Core.Value, out var blobCoreComponent))
            return;
        blobCoreComponent.BlobTiles.Remove(uid);
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
        if (component.Core == null || !_blobCoreQuery.TryComp(component.Core.Value, out var blobCoreComponent))
            return;

        if (blobCoreComponent.CurrentChem == BlobChemType.ElectromagneticWeb)
        {
            _empSystem.EmpPulse(_transform.GetMapCoordinates(uid), 3f, 50f, 3f);
        }
    }

    protected override  void TryRemove(EntityUid target, EntityUid coreUid, BlobTileComponent tile, BlobCoreComponent core)
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
            if (_blobCoreQuery.TryComp(tile.Core, out var blobCoreComponent) && blobCoreComponent.Observer != null)
            {
                _popup.PopupCoordinates(Loc.GetString("blob-get-resource", ("point", returnCost)),
                    xform.Coordinates,
                    blobCoreComponent.Observer.Value,
                    PopupType.Large);
            }
            _blobCoreSystem.ChangeBlobPoint(coreUid, returnCost, core);
        }
    }

    private void OnPulsed(EntityUid uid, BlobTileComponent component, BlobTileGetPulseEvent args)
    {

        if (!TryComp<BlobTileComponent>(uid, out var blobTileComponent) || blobTileComponent.Core == null ||
            !_blobCoreQuery.TryComp(blobTileComponent.Core.Value, out var blobCoreComponent))
            return;

        if (blobCoreComponent.CurrentChem == BlobChemType.RegenerativeMateria)
        {
            var healCore = new DamageSpecifier();
            foreach (var keyValuePair in component.HealthOfPulse.DamageDict)
            {
                healCore.DamageDict.Add(keyValuePair.Key, keyValuePair.Value * 5);
            }
            _damageableSystem.TryChangeDamage(uid, healCore);
        }
        else
        {
            _damageableSystem.TryChangeDamage(uid, component.HealthOfPulse);
        }

        if (!args.Handled)
            return;

        var xform = Transform(uid);

        if (!TryComp<MapGridComponent>(xform.GridUid, out var grid))
        {
            return;
        }

        var mobTile = _mapSystem.GetTileRef(xform.GridUid.Value, grid, xform.Coordinates);

        var mobAdjacentTiles = new[]
        {
            mobTile.GridIndices.Offset(Direction.East),
            mobTile.GridIndices.Offset(Direction.West),
            mobTile.GridIndices.Offset(Direction.North),
            mobTile.GridIndices.Offset(Direction.South)
        };

        var localPos = xform.Coordinates.Position;

        var radius = 1.0f;

        var innerTiles = _mapSystem.GetLocalTilesIntersecting(xform.GridUid.Value, grid,
            new Box2(localPos + new Vector2(-radius, -radius), localPos + new Vector2(radius, radius))).ToArray();

        foreach (var innerTile in innerTiles)
        {
            if (!mobAdjacentTiles.Contains(innerTile.GridIndices))
            {
                continue;
            }

            foreach (var ent in _mapSystem.GetAnchoredEntities(xform.GridUid.Value, grid, innerTile.GridIndices))
            {
                if (!HasComp<DestructibleComponent>(ent) || !HasComp<ConstructionComponent>(ent))
                    continue;
                _damageableSystem.TryChangeDamage(ent, blobCoreComponent.ChemDamageDict[blobCoreComponent.CurrentChem]);
                _audioSystem.PlayPvs(blobCoreComponent.AttackSound, uid, AudioParams.Default);
                args.Handled = true;
                return;
            }
            var spawn = true;
            foreach (var ent in _mapSystem.GetAnchoredEntities(xform.GridUid.Value, grid, innerTile.GridIndices))
            {
                if (!HasComp<BlobTileComponent>(ent))
                    continue;
                spawn = false;
                break;
            }

            if (!spawn)
                continue;

            var location = _mapSystem.ToCoordinates(xform.GridUid.Value, innerTile.GridIndices, grid);

            if (_blobCoreSystem.TransformBlobTile(null,
                    blobTileComponent.Core.Value,
                    blobCoreComponent.NormalBlobTile,
                    location,
                    blobCoreComponent,
                    false))
                return;
        }
    }

    protected override void TryUpgrade(EntityUid target, EntityUid user, EntityUid coreUid, BlobTileComponent tile, BlobCoreComponent core)
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
}
