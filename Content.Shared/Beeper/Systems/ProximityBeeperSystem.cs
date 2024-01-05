using Content.Shared.Beeper.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Pinpointer;
using Content.Shared.PowerCell;
using Content.Shared.ProximityDetection;
using Content.Shared.ProximityDetection.Components;
using Content.Shared.ProximityDetection.Systems;

namespace Content.Shared.Beeper.Systems;

/// <summary>
/// This handles logic for implementing proximity beeper as a handheld tool />
/// </summary>
public sealed class ProximityBeeperSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPowerCellSystem _powerCell = default!;
    [Dependency] private readonly ProximityDetectionSystem _proximity = default!;
    [Dependency] private readonly BeeperSystem _beeper = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ProximityBeeperComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<ProximityBeeperComponent, PowerCellSlotEmptyEvent>(OnPowerCellSlotEmpty);
        SubscribeLocalEvent<ProximityBeeperComponent, NewProximityTargetEvent>(OnNewProximityTarget);
        SubscribeLocalEvent<ProximityBeeperComponent, ProximityTargetUpdatedEvent>(OnProximityTargetUpdate);
    }

    private void OnProximityTargetUpdate(EntityUid owner, ProximityBeeperComponent proxBeeper, ref ProximityTargetUpdatedEvent args)
    {
        if (!TryComp<BeeperComponent>(owner, out var beeper))
            return;
        if (args.Target == null)
        {
            _beeper.SetEnable(owner, false, beeper);
            return;
        }
        _beeper.SetIntervalScaling(owner,args.Distance/args.Detector.Range, beeper);
        _beeper.SetEnable(owner, true, beeper);
    }

    private void OnNewProximityTarget(EntityUid owner, ProximityBeeperComponent proxBeeper, ref NewProximityTargetEvent args)
    {
        _beeper.SetEnable(owner, args.Target != null);
    }

    private void OnUseInHand(EntityUid uid, ProximityBeeperComponent proxBeeper, UseInHandEvent args)
    {
        if (args.Handled)
            return;
        args.Handled = TryToggle(uid, proxBeeper, user: args.User);
    }

    private void OnPowerCellSlotEmpty(EntityUid uid, ProximityBeeperComponent beeper, ref PowerCellSlotEmptyEvent args)
    {
        if (_proximity.GetEnable(uid))
            TryDisable(uid);
    }
    public bool TryEnable(EntityUid owner, BeeperComponent? beeper = null, ProximityDetectorComponent? detector = null,
        PowerCellDrawComponent? draw = null,EntityUid? user = null)
    {
        if (!Resolve(owner, ref beeper, ref detector))
            return false;
        if (Resolve(owner, ref draw, false) && !_powerCell.HasActivatableCharge(owner, battery: draw, user: user))
                return false;
        Enable(owner, beeper, detector, draw);
        return true;
    }
    private void Enable(EntityUid owner, BeeperComponent beeper,
        ProximityDetectorComponent detector, PowerCellDrawComponent? draw)
    {
        _proximity.SetEnable(owner, true, detector);
        _appearance.SetData(owner, ProximityBeeperVisuals.Enabled, true);
        _powerCell.SetPowerCellDrawEnabled(owner, true, draw);
    }


    /// <summary>
    /// Disables the proximity beeper
    /// </summary>
    public bool TryDisable(EntityUid owner,BeeperComponent? beeper = null, ProximityDetectorComponent? detector = null, PowerCellDrawComponent? draw = null)
    {
        if (!Resolve(owner, ref beeper, ref detector))
            return false;

        if (!detector.Enabled)
            return false;
        Disable(owner, beeper, detector, draw);
        return true;
    }
    private void Disable(EntityUid owner, BeeperComponent beeper,
        ProximityDetectorComponent detector, PowerCellDrawComponent? draw)
    {
        _proximity.SetEnable(owner, false, detector);
        _appearance.SetData(owner, ProximityBeeperVisuals.Enabled, false);
        _beeper.SetEnable(owner, false, beeper);
        _powerCell.SetPowerCellDrawEnabled(owner, false, draw);
    }

    /// <summary>
    /// toggles the proximity beeper
    /// </summary>
    public bool TryToggle(EntityUid owner, ProximityBeeperComponent? proxBeeper = null, BeeperComponent? beeper = null, ProximityDetectorComponent? detector = null,
        PowerCellDrawComponent? draw = null, EntityUid? user = null)
    {
        if (!Resolve(owner,  ref proxBeeper, ref beeper, ref detector))
            return false;

        return detector.Enabled
            ? TryDisable(owner, beeper, detector, draw)
            : TryEnable(owner, beeper, detector, draw,user);
    }
}
