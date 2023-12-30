using Content.Server.Anomaly.Components;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Station.Components;
using Content.Shared.Anomaly;
using Content.Shared.CCVar;
using Content.Shared.Materials;
using Content.Shared.Radio;
using Robust.Shared.Audio;
using Content.Shared.Physics;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Map;
using System.Numerics;

namespace Content.Server.Anomaly;

/// <summary>
/// This handles anomalous vessel as well as
/// the calculations for how many points they
/// should produce.
/// </summary>
public sealed partial class AnomalySystem
{

    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private void InitializeGenerator()
    {
        SubscribeLocalEvent<AnomalyGeneratorComponent, BoundUIOpenedEvent>(OnGeneratorBUIOpened);
        SubscribeLocalEvent<AnomalyGeneratorComponent, MaterialAmountChangedEvent>(OnGeneratorMaterialAmountChanged);
        SubscribeLocalEvent<AnomalyGeneratorComponent, AnomalyGeneratorGenerateButtonPressedEvent>(OnGenerateButtonPressed);
        SubscribeLocalEvent<AnomalyGeneratorComponent, PowerChangedEvent>(OnGeneratorPowerChanged);
        SubscribeLocalEvent<AnomalyGeneratorComponent, EntityUnpausedEvent>(OnGeneratorUnpaused);
        SubscribeLocalEvent<GeneratingAnomalyGeneratorComponent, ComponentStartup>(OnGeneratingStartup);
        SubscribeLocalEvent<GeneratingAnomalyGeneratorComponent, EntityUnpausedEvent>(OnGeneratingUnpaused);
    }

    private void OnGeneratorPowerChanged(EntityUid uid, AnomalyGeneratorComponent component, ref PowerChangedEvent args)
    {
        _ambient.SetAmbience(uid, args.Powered);
    }

    private void OnGeneratorBUIOpened(EntityUid uid, AnomalyGeneratorComponent component, BoundUIOpenedEvent args)
    {
        UpdateGeneratorUi(uid, component);
    }

    private void OnGeneratorMaterialAmountChanged(EntityUid uid, AnomalyGeneratorComponent component, ref MaterialAmountChangedEvent args)
    {
        UpdateGeneratorUi(uid, component);
    }

    private void OnGenerateButtonPressed(EntityUid uid, AnomalyGeneratorComponent component, AnomalyGeneratorGenerateButtonPressedEvent args)
    {
        TryGeneratorCreateAnomaly(uid, component);
    }

    private void OnGeneratorUnpaused(EntityUid uid, AnomalyGeneratorComponent component, ref EntityUnpausedEvent args)
    {
        component.CooldownEndTime += args.PausedTime;
    }

    public void UpdateGeneratorUi(EntityUid uid, AnomalyGeneratorComponent component)
    {
        var materialAmount = _material.GetMaterialAmount(uid, component.RequiredMaterial);

        var state = new AnomalyGeneratorUserInterfaceState(component.CooldownEndTime, materialAmount, component.MaterialPerAnomaly);
        _ui.TrySetUiState(uid, AnomalyGeneratorUiKey.Key, state);
    }

    public void TryGeneratorCreateAnomaly(EntityUid uid, AnomalyGeneratorComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!this.IsPowered(uid, EntityManager))
            return;

        if (Timing.CurTime < component.CooldownEndTime)
            return;

        if (!_material.TryChangeMaterialAmount(uid, component.RequiredMaterial, -component.MaterialPerAnomaly))
            return;

        var generating = EnsureComp<GeneratingAnomalyGeneratorComponent>(uid);
        generating.EndTime = Timing.CurTime + component.GenerationLength;
        generating.AudioStream = Audio.PlayPvs(component.GeneratingSound, uid, AudioParams.Default.WithLoop(true))?.Entity;
        component.CooldownEndTime = Timing.CurTime + component.CooldownLength;
        UpdateGeneratorUi(uid, component);
    }

    public void SpawnOnRandomGridLocation(EntityUid grid, string toSpawn)
    {
        if (!TryComp<MapGridComponent>(grid, out var gridComp))
            return;

        var xform = Transform(grid);

        var targetCoords = xform.Coordinates;
        var gridBounds = gridComp.LocalAABB.Scale(_configuration.GetCVar(CCVars.AnomalyGenerationGridBoundsScale));

        for (var i = 0; i < 25; i++)
        {
            var randomX = Random.Next((int) gridBounds.Left, (int) gridBounds.Right);
            var randomY = Random.Next((int) gridBounds.Bottom, (int)gridBounds.Top);

            var tile = new Vector2i(randomX, randomY);

            // no air-blocked areas.
            if (_atmosphere.IsTileSpace(grid, xform.MapUid, tile, mapGridComp: gridComp) ||
                _atmosphere.IsTileAirBlocked(grid, tile, mapGridComp: gridComp))
            {
                continue;
            }

            // don't spawn inside of solid objects
            var physQuery = GetEntityQuery<PhysicsComponent>();
            var valid = true;
            foreach (var ent in gridComp.GetAnchoredEntities(tile))
            {
                if (!physQuery.TryGetComponent(ent, out var body))
                    continue;
                if (body.BodyType != BodyType.Static ||
                    !body.Hard ||
                    (body.CollisionLayer & (int) CollisionGroup.Impassable) == 0)
                    continue;

                valid = false;
                break;
            }
            if (!valid)
                continue;

            // don't spawn in AntiAnomalyZones
            var antiAnomalyZonesQueue = AllEntityQuery<AntiAnomalyZoneComponent>();
            while (antiAnomalyZonesQueue.MoveNext(out var uid, out var zone))
            {
                var zoneTile = _transform.GetGridTilePositionOrDefault(uid, gridComp);

                var delta = (zoneTile - tile);
                if (delta.LengthSquared < zone.ZoneRadius * zone.ZoneRadius)
                {
                    valid = false;
                    break;
                }
            }
            if (!valid)
                continue;

            targetCoords = gridComp.GridTileToLocal(tile);
            break;
        }

        Spawn(toSpawn, targetCoords);
    }

    private void OnGeneratingStartup(EntityUid uid, GeneratingAnomalyGeneratorComponent component, ComponentStartup args)
    {
        Appearance.SetData(uid, AnomalyGeneratorVisuals.Generating, true);
    }

    private void OnGeneratingUnpaused(EntityUid uid, GeneratingAnomalyGeneratorComponent component, ref EntityUnpausedEvent args)
    {
        component.EndTime += args.PausedTime;
    }

    private void OnGeneratingFinished(EntityUid uid, AnomalyGeneratorComponent component)
    {
        var xform = Transform(uid);

        if (_station.GetStationInMap(xform.MapID) is not { } station ||
            !TryComp<StationDataComponent>(station, out var data) ||
            _station.GetLargestGrid(data) is not { } grid)
        {
            if (xform.GridUid == null)
                return;
            grid = xform.GridUid.Value;
        }

        SpawnOnRandomGridLocation(grid, component.SpawnerPrototype);
        RemComp<GeneratingAnomalyGeneratorComponent>(uid);
        Appearance.SetData(uid, AnomalyGeneratorVisuals.Generating, false);
        Audio.PlayPvs(component.GeneratingFinishedSound, uid);

        var message = Loc.GetString("anomaly-generator-announcement");
        _radio.SendRadioMessage(uid, message, _prototype.Index<RadioChannelPrototype>(component.ScienceChannel), uid);
    }

    private void UpdateGenerator()
    {
        var query = EntityQueryEnumerator<GeneratingAnomalyGeneratorComponent, AnomalyGeneratorComponent>();
        while (query.MoveNext(out var ent, out var active, out var gen))
        {
            if (Timing.CurTime < active.EndTime)
                continue;

            active.AudioStream = _audio.Stop(active.AudioStream);
            OnGeneratingFinished(ent, gen);
        }
    }
}
