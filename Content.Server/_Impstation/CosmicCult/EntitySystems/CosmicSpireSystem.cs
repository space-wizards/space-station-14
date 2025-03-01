using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.EntitySystems;
using Robust.Server.GameObjects;
using Content.Server.Atmos.Piping.Unary.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping.Unary.Components;
using Content.Shared._Impstation.CosmicCult.Components;
using Content.Server.Audio;
using Content.Server.Popups;

namespace Content.Server._Impstation.CosmicCult.EntitySystems;

public sealed class CosmicSpireSystem : EntitySystem
{
    [Dependency] private readonly GasVentScrubberSystem _scrub = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly AmbientSoundSystem _ambient = default!;
    [Dependency] private readonly SharedPointLightSystem _lights = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly CosmicCultRuleSystem _cosmicRule = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CosmicSpireComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        SubscribeLocalEvent<CosmicSpireComponent, AtmosDeviceUpdateEvent>(OnDeviceUpdated);
        SubscribeLocalEvent<CosmicSpireComponent, GasAnalyzerScanEvent>(OnSpireAnalyzed);
    }

    private void OnAnchorChanged(EntityUid uid, CosmicSpireComponent comp, ref AnchorStateChangedEvent args)
    {
        if (args.Anchored)
        {
            comp.Enabled = true;
            UpdateSpireAppearance(uid, SpireStatus.On);
        }
        if (!args.Anchored)
        {
            comp.Enabled = false;
            UpdateSpireAppearance(uid, SpireStatus.Off);
        }
        _ambient.SetAmbience(uid, comp.Enabled);
        _lights.SetEnabled(uid, comp.Enabled);
    }

    private void OnDeviceUpdated(EntityUid uid, CosmicSpireComponent comp, ref AtmosDeviceUpdateEvent args)
    {
        if (!comp.Enabled)
            return;
        if (args.Grid is not { } grid)
            return;
        var timeDelta = args.dt;
        var position = _transform.GetGridTilePositionOrDefault(uid);
        var environment = _atmos.GetTileMixture(grid, args.Map, position, true);
        var running = Drain(timeDelta, comp, environment);
        if (!running)
            return;
        var enumerator = _atmos.GetAdjacentTileMixtures(grid, position, false, true);
        while (enumerator.MoveNext(out var adjacent))
        {
            Drain(timeDelta, comp, adjacent);
        }
        if (comp.Storage.TotalMoles >= comp.DrainThreshHold)
        {
            _popup.PopupCoordinates(Loc.GetString("cosmiccult-spire-entropy"), Transform(uid).Coordinates);
            comp.Storage.Clear();
            Spawn(comp.SpawnVFX, Transform(uid).Coordinates);
            Spawn(comp.EntropyMote, Transform(uid).Coordinates);
            _cosmicRule.EntropySiphoned++;
        }
    }

    private bool Drain(float timeDelta, CosmicSpireComponent comp, GasMixture? tile)
    {
        return _scrub.Scrub(timeDelta, comp.DrainRate * _atmos.PumpSpeedup(), ScrubberPumpDirection.Scrubbing, comp.DrainGases, tile, comp.Storage);
    }

    private void OnSpireAnalyzed(EntityUid uid, CosmicSpireComponent comp, GasAnalyzerScanEvent args)
    {
        args.GasMixtures ??= [];
        args.GasMixtures.Add((Name(uid), comp.Storage));
    }

    private void UpdateSpireAppearance(EntityUid uid, SpireStatus status)
    {
        if (TryComp<AppearanceComponent>(uid, out var appearance))
        {
            _appearance.SetData(uid, SpireVisuals.Status, status, appearance);
        }
    }

}
