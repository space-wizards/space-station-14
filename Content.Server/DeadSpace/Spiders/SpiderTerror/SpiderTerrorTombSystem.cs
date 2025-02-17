// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Server.DeadSpace.Spiders.SpiderTerror.Components;
using Content.Shared.Maps;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Map;
using System.Numerics;
using Robust.Shared.Timing;
using System.Linq;
using Content.Server.Station.Systems;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.Examine;
using Robust.Shared.Map.Components;

namespace Content.Server.DeadSpace.Spiders.SpiderTerror;

public sealed class SpiderTerrorTombSystem : EntitySystem
{
    [Dependency] private readonly SpiderTerrorConditionsSystem _spiderTerrorConditions = default!;
    [Dependency] private readonly ITileDefinitionManager _tiledef = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly TileSystem _tile = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpiderTerrorTombComponent, ComponentStartup>(OnComponentStartUp);
        SubscribeLocalEvent<SpiderTerrorTombComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<SpiderTerrorTombComponent, ExaminedEvent>(OnExamine);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var tombQuery = EntityQueryEnumerator<SpiderTerrorTombComponent>();
        while (tombQuery.MoveNext(out var ent, out var tomb))
        {
            if (_gameTiming.CurTime > tomb.TimeUtilRegen)
            {
                RegenReagent(ent, tomb);
            }
        }
    }
    private void OnExamine(EntityUid uid, SpiderTerrorTombComponent component, ExaminedEvent args)
    {
        if (!HasComp<SpiderTerrorComponent>(args.Examiner))
            return;

        var bloodVolume = component.Reagent;
        args.PushMarkup(Loc.GetString($"Содержит [color=red]{bloodVolume} крови[/color]."));
        return;
    }
    private void OnComponentStartUp(EntityUid uid, SpiderTerrorTombComponent component, ComponentStartup args)
    {
        if (!TryComp<TransformComponent>(uid, out var xform))
            return;

        component.OldMaxReagent = component.MaxReagent;
        component.Station = _station.GetStationInMap(xform.MapID);

        var tile = (ContentTileDefinition)_tiledef[component.TileId];

        if (component.Station != null)
        {
            foreach (var tileref in GetTiles(uid, component))
            {
                if (!_spiderTerrorConditions.IsContains(tileref, component.Station.Value))
                {
                    _tile.ReplaceTile(tileref, tile);
                    component.TileRefs.Add(tileref);
                    _spiderTerrorConditions.TryAddTile(tileref, component.Station.Value);
                }
            }

            foreach (var tileref in GetTiles(uid, component))
            {
                component.TileRefs.Add(tileref);
                _spiderTerrorConditions.TryAddTile(tileref, component.Station.Value);
            }

            var stageCapture = new SpiderTerrorAttackStationEvent(component.Station.Value);
            var ruleQuery = AllEntityQuery<SpiderTerrorRuleComponent>();
            while (ruleQuery.MoveNext(out var ruleUid, out _))
            {
                RaiseLocalEvent(ruleUid, ref stageCapture);
            }

            var entities = _lookup.GetEntitiesInRange<SpiderTerrorTombComponent>(_transform.GetMapCoordinates(uid, Transform(uid)), component.Range);

            foreach (var (entity, comp) in entities)
            {
                comp.MaxReagent = component.OldMaxReagent / entities.Count;
                Console.WriteLine(entity);
            }
        }
    }

    private void OnComponentShutdown(EntityUid uid, SpiderTerrorTombComponent component, ComponentShutdown args)
    {
        if (component.Station == null)
            return;

        foreach (var tile in component.TileRefs)
        {
            _spiderTerrorConditions.RemTile(tile, component.Station.Value);
        }
    }

    private IEnumerable<TileRef> GetTiles(EntityUid uid, SpiderTerrorTombComponent component)
    {
        var xform = Transform(uid);
        IEnumerable<TileRef> tilerefs = Enumerable.Empty<TileRef>();

        if (!TryComp<MapGridComponent>(xform.GridUid, out var grid))
            return tilerefs;

        var radius = component.Range;
        var localpos = xform.Coordinates.Position;
        tilerefs = grid.GetLocalTilesIntersecting(new Box2(localpos + new Vector2(-radius, -radius), localpos + new Vector2(radius, radius)));

        return tilerefs;
    }

    private void RegenReagent(EntityUid uid, SpiderTerrorTombComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.Reagent >= component.MaxReagent)
            return;

        AddReagent(uid, component.Regen, component);
        component.TimeUtilRegen = _gameTiming.CurTime + TimeSpan.FromSeconds(1);
    }
    public void AddReagent(EntityUid uid, float reagent, SpiderTerrorTombComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.Reagent += reagent;
    }

}
