using Content.Shared.Interaction.Events;
using Content.Shared.Pinpointer;
using Content.Shared.PowerCell;
using Content.Shared.ProximityDetection.Components;

namespace Content.Shared.ProximityDetection.Systems;

/// <summary>
/// This handles logic for implementing proximity beeper as a handheld tool />
/// </summary>
public sealed class ToolProximityBeeperSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPowerCellSystem _powerCell = default!;
    [Dependency] private readonly ProximityBeeperSystem _proximityBeeper = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ProximityBeeperComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<ProximityBeeperComponent, PowerCellSlotEmptyEvent>(OnPowerCellSlotEmpty);
    }
    private void OnUseInHand(EntityUid uid, ProximityBeeperComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryToggle(uid, component, args.User);
    }

    private void OnPowerCellSlotEmpty(EntityUid uid, ProximityBeeperComponent component, ref PowerCellSlotEmptyEvent args)
    {
        if (component is {Enabled: true, DrawsPower: true})
            TryDisable(uid, component);
    }
    public bool TryEnable(EntityUid uid, ProximityBeeperComponent? component = null, EntityUid? user = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (TryComp<PowerCellDrawComponent>(uid, out var draw) && component.DrawsPower)
        {
            if (!_powerCell.HasActivatableCharge(uid, battery: draw, user: user))
                return false;
            _powerCell.SetPowerCellDrawEnabled(uid, true, draw);
        }
        _appearance.SetData(uid, ProximityBeeperVisuals.Enabled, true);
        _proximityBeeper.SetEnable(uid, true, component);
        return true;
    }

    /// <summary>
    /// Disables the proximity beeper
    /// </summary>
    public bool TryDisable(EntityUid uid, ProximityBeeperComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!component.Enabled)
            return false;
        _appearance.SetData(uid, ProximityBeeperVisuals.Enabled, false);
        if (component.DrawsPower)
            _powerCell.SetPowerCellDrawEnabled(uid, false );
        _proximityBeeper.SetEnable(uid, false, component);
        return true;
    }

    /// <summary>
    /// toggles the proximity beeper
    /// </summary>
    public bool TryToggle(EntityUid uid, ProximityBeeperComponent? component = null, EntityUid? user = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        return component.Enabled
            ? TryDisable(uid, component)
            : TryEnable(uid, component, user);
    }
}
