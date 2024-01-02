using System.Linq;
using System.Numerics;
using Content.Server.Salvage.Magnet;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Components;
using Content.Shared.Radio;
using Content.Shared.Salvage.Magnet;
using Robust.Server.Maps;
using Robust.Shared.Map;

namespace Content.Server.Salvage;

public sealed partial class SalvageSystem
{
    [ValidatePrototypeId<RadioChannelPrototype>]
    private const string MagnetChannel = "Supply";

    private EntityQuery<SalvageMobRestrictionsComponent> _salvMobQuery;

    private void InitializeMagnet()
    {
        _salvMobQuery = GetEntityQuery<SalvageMobRestrictionsComponent>();

        SubscribeLocalEvent<SalvageMagnetDataComponent, MapInitEvent>(OnMagnetDataMapInit);

        SubscribeLocalEvent<SalvageMagnetTargetComponent, GridSplitEvent>(OnMagnetTargetSplit);

        SubscribeLocalEvent<SalvageMagnetComponent, MagnetClaimOfferEvent>(OnMagnetClaim);
        SubscribeLocalEvent<SalvageMagnetComponent, ComponentStartup>(OnMagnetStartup);
        SubscribeLocalEvent<SalvageMagnetComponent, AnchorStateChangedEvent>(OnMagnetAnchored);
    }

    private void OnMagnetClaim(EntityUid uid, SalvageMagnetComponent component, ref MagnetClaimOfferEvent args)
    {
        var player = args.Session.AttachedEntity;

        if (player is null)
            return;

        var station = _station.GetOwningStation(uid);

        if (!TryComp(station, out SalvageMagnetDataComponent? dataComp) ||
            dataComp.EndTime != null)
        {
            return;
        }

        TakeMagnetOffer((station.Value, dataComp), args.Index, (uid, component));
    }

    private void OnMagnetStartup(EntityUid uid, SalvageMagnetComponent component, ComponentStartup args)
    {
        UpdateMagnetUI((uid, component), Transform(uid));
    }

    private void OnMagnetAnchored(EntityUid uid, SalvageMagnetComponent component, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
            return;

        UpdateMagnetUI((uid, component), args.Transform);
    }

    private void OnMagnetDataMapInit(EntityUid uid, SalvageMagnetDataComponent component, ref MapInitEvent args)
    {
        CreateMagnetOffers((uid, component));
    }

    private void OnMagnetTargetSplit(EntityUid uid, SalvageMagnetTargetComponent component, ref GridSplitEvent args)
    {
        // Don't think I'm not onto you people splitting to make new grids.
        if (TryComp(component.DataTarget, out SalvageMagnetDataComponent? dataComp))
        {
            foreach (var gridUid in args.NewGrids)
            {
                dataComp.ActiveEntities?.Add(gridUid);
            }
        }
    }

    private void UpdateMagnet()
    {
        var dataQuery = EntityQueryEnumerator<SalvageMagnetDataComponent>();
        var curTime = _timing.CurTime;

        while (dataQuery.MoveNext(out var uid, out var magnetData))
        {
            // Magnet currently active.
            if (magnetData.EndTime != null)
            {
                if (magnetData.EndTime.Value < curTime)
                {
                    EndMagnet((uid, magnetData));
                }
                else if (!magnetData.Announced && (magnetData.EndTime.Value - curTime).TotalSeconds < 30)
                {
                    var magnet = GetMagnet((uid, magnetData));

                    if (magnet != null)
                    {
                        Report(magnet.Value.Owner, MagnetChannel,
                            "salvage-system-announcement-losing",
                            ("timeLeft", (magnetData.EndTime.Value - curTime).TotalSeconds));
                    }

                    magnetData.Announced = true;
                }
            }
            if (magnetData.NextOffer < curTime)
            {
                CreateMagnetOffers((uid, magnetData));
            }
        }
    }

    /// <summary>
    /// Ends the magnet attachment and deletes the relevant grids.
    /// </summary>
    private void EndMagnet(Entity<SalvageMagnetDataComponent> data)
    {
        if (data.Comp.ActiveEntities != null)
        {
            // Handle mobrestrictions getting deleted
            var query = AllEntityQuery<SalvageMobRestrictionsComponent>();

            while (query.MoveNext(out var salvUid, out var salvMob))
            {
                if (data.Comp.ActiveEntities.Contains(salvMob.LinkedEntity))
                {
                    QueueDel(salvUid);
                }
            }

            // Uhh yeah don't delete mobs or whatever
            var mobQuery = AllEntityQuery<HumanoidAppearanceComponent, MobStateComponent, TransformComponent>();

            while (mobQuery.MoveNext(out var mobUid, out _, out _, out var xform))
            {
                if (xform.GridUid == null || !data.Comp.ActiveEntities.Contains(xform.GridUid.Value) || xform.MapUid == null)
                    continue;

                _transform.SetParent(mobUid, xform.MapUid.Value);
            }

            // Go and cleanup the active ents.
            foreach (var ent in data.Comp.ActiveEntities)
            {
                Del(ent);
            }

            data.Comp.ActiveEntities = null;
        }

        data.Comp.EndTime = null;
        UpdateMagnetUIs(data);
    }

    private void CreateMagnetOffers(Entity<SalvageMagnetDataComponent> data)
    {
        data.Comp.Offered.Clear();

        for (var i = 0; i < data.Comp.OfferCount; i++)
        {
            var seed = _random.Next();

            // Fuck with the seed to mix wrecks and asteroids.
            seed = (int) (seed / 10f) * 10;

            if (i >= data.Comp.OfferCount / 2)
            {
                seed++;
            }

            data.Comp.Offered.Add(seed);
        }

        data.Comp.NextOffer = _timing.CurTime + data.Comp.OfferCooldown;
        UpdateMagnetUIs(data);
    }

    // Just need something to announce.
    private Entity<SalvageMagnetComponent>? GetMagnet(Entity<SalvageMagnetDataComponent> data)
    {
        var query = AllEntityQuery<SalvageMagnetComponent, TransformComponent>();

        while (query.MoveNext(out var magnetUid, out var magnet, out var xform))
        {
            var stationUid = _station.GetOwningStation(magnetUid, xform);

            if (stationUid != data.Owner)
                continue;

            return (magnetUid, magnet);
        }

        return null;
    }

    private void UpdateMagnetUI(Entity<SalvageMagnetComponent> entity, TransformComponent xform)
    {
        var station = _station.GetOwningStation(entity, xform);

        if (!TryComp(station, out SalvageMagnetDataComponent? dataComp))
            return;

        _ui.TrySetUiState(entity, SalvageMagnetUiKey.Key,
            new SalvageMagnetBoundUserInterfaceState(dataComp.Offered)
            {
                Cooldown = dataComp.OfferCooldown,
                Duration = dataComp.ActiveTime,
                EndTime = dataComp.EndTime,
                NextOffer = dataComp.NextOffer,
                ActiveSeed = dataComp.ActiveSeed,
            });
    }

    private void UpdateMagnetUIs(Entity<SalvageMagnetDataComponent> data)
    {
        var query = AllEntityQuery<SalvageMagnetComponent, TransformComponent>();

        while (query.MoveNext(out var magnetUid, out var magnet, out var xform))
        {
            var station = _station.GetOwningStation(magnetUid, xform);

            if (station != data.Owner)
                continue;

            _ui.TrySetUiState(magnetUid, SalvageMagnetUiKey.Key,
                new SalvageMagnetBoundUserInterfaceState(data.Comp.Offered)
                {
                    Cooldown = data.Comp.OfferCooldown,
                    Duration = data.Comp.ActiveTime,
                    EndTime = data.Comp.EndTime,
                    NextOffer = data.Comp.NextOffer,
                    ActiveSeed = data.Comp.ActiveSeed,
                });
        }
    }

    public void TakeMagnetOffer(Entity<SalvageMagnetDataComponent> data, int index, Entity<SalvageMagnetComponent> magnet)
    {
        var seed = data.Comp.Offered[index];

        var offering = GetSalvageOffering(seed);
        var salvMap = _mapManager.CreateMap();

        List<EntityUid> salvageEnts;

        switch (offering)
        {
            case AsteroidOffering asteroid:
                var grid = _mapManager.CreateGrid(salvMap);
                _dungeon.GenerateDungeon(asteroid.DungeonConfig, grid.Owner, grid, Vector2i.Zero, seed);
                break;
            case SalvageOffering wreck:
                var salvageProto = wreck.SalvageMap;

                var opts = new MapLoadOptions
                {
                    Offset = new Vector2(0, 0)
                };

                if (!_map.TryLoad(salvMap, salvageProto.MapPath.ToString(), out var roots, opts))
                {
                    Report(magnet, MagnetChannel, "salvage-system-announcement-spawn-debris-disintegrated");
                    _mapManager.DeleteMap(salvMap);
                    return;
                }

                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        data.Comp.ActiveSeed = seed;

        Box2? bounds = null;
        var mapXform = _xformQuery.GetComponent(_mapManager.GetMapEntityId(salvMap));

        if (mapXform.ChildCount == 0)
        {
            Report(magnet.Owner, MagnetChannel, "salvage-system-announcement-spawn-no-debris-available");
            return;
        }

        var mapChildren = mapXform.ChildEnumerator;

        while (mapChildren.MoveNext(out var mapChild))
        {
            var childAABB = _transform.GetWorldMatrix(mapChild).TransformBox(_gridQuery.GetComponent(mapChild).LocalAABB);
            bounds = bounds?.Union(childAABB) ?? childAABB;
        }

        if (!TryGetSalvagePlacementLocation(magnet, bounds!.Value, out var spawnLocation, out var spawnAngle))
        {
            Report(magnet.Owner, MagnetChannel, "salvage-system-announcement-spawn-no-debris-available");
            _mapManager.DeleteMap(salvMap);
            return;
        }

        data.Comp.ActiveEntities = null;

        // It worked, move it into position and cleanup values.
        while (mapChildren.MoveNext(out var mapChild))
        {
            var salvXForm = _xformQuery.GetComponent(mapChild);
            var localPos = salvXForm.LocalPosition;
            _transform.SetParent(mapChild, salvXForm, _mapManager.GetMapEntityId(spawnLocation.MapId));
            _transform.SetWorldPositionRotation(mapChild, spawnLocation.Position + localPos, spawnAngle, salvXForm);

            data.Comp.ActiveEntities ??= new List<EntityUid>();
            data.Comp.ActiveEntities?.Add(mapChild);

            // Handle mob restrictions
            var children = salvXForm.ChildEnumerator;

            while (children.MoveNext(out var child))
            {
                if (!_salvMobQuery.TryGetComponent(child, out var salvMob))
                    continue;

                salvMob.LinkedEntity = mapChild;
            }
        }

        Report(magnet.Owner, MagnetChannel, "salvage-system-announcement-arrived", ("timeLeft", data.Comp.ActiveTime.TotalSeconds));
        _mapManager.DeleteMap(salvMap);

        data.Comp.Announced = false;
        data.Comp.EndTime = _timing.CurTime + data.Comp.ActiveTime;
        data.Comp.NextOffer = data.Comp.EndTime.Value;
        var active = new SalvageMagnetActivatedEvent()
        {
            Magnet = magnet,
        };

        RaiseLocalEvent(ref active);
        UpdateMagnetUIs(data);
    }

    private bool TryGetSalvagePlacementLocation(Entity<SalvageMagnetComponent> magnet, Box2 bounds, out MapCoordinates coords, out Angle angle)
    {
        const float OffsetRadiusMax = 32f;

        var xform = Transform(magnet.Owner);
        var smallestBound = (bounds.Height < bounds.Width
            ? bounds.Height
            : bounds.Width) / 2f;
        var maxRadius = OffsetRadiusMax + smallestBound;

        angle = Angle.Zero;
        coords = new EntityCoordinates(magnet.Owner, new Vector2(0, -maxRadius)).ToMap(EntityManager, _transform);

        if (xform.GridUid is not null)
            angle = _transform.GetWorldRotation(Transform(xform.GridUid.Value));

        // Thanks 20kdc
        for (var i = 0; i < 20; i++)
        {
            var randomRadius = _random.NextFloat(OffsetRadiusMax);
            var randomOffset = _random.NextAngle().ToVec() * randomRadius;
            var finalCoords = new MapCoordinates(coords.Position + randomOffset, coords.MapId);

            var box2 = Box2.CenteredAround(finalCoords.Position, bounds.Size);
            var box2Rot = new Box2Rotated(box2, angle, finalCoords.Position);

            // This doesn't stop it from spawning on top of random things in space
            // Might be better like this, ghosts could stop it before
            if (_mapManager.FindGridsIntersecting(finalCoords.MapId, box2Rot).Any())
                continue;
            coords = finalCoords;
            return true;
        }
        return false;
    }
}

[ByRefEvent]
public record struct SalvageMagnetActivatedEvent
{
    public EntityUid Magnet;
}
