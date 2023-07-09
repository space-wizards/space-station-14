using Content.Server.PowerCell;
using Content.Shared.PowerCell;
using Robust.Shared.Containers;

namespace Content.Server.UserInterface;

public sealed partial class ActivatableUISystem
{
    [Dependency] private readonly PowerCellSystem _cell = default!;

    private void InitializePower()
    {
        SubscribeLocalEvent<ActivatableUIRequiresPowerCellComponent, ActivatableUIOpenAttemptEvent>(OnBatteryOpenAttempt);
        SubscribeLocalEvent<ActivatableUIRequiresPowerCellComponent, BoundUIOpenedEvent>(OnBatteryOpened);
        SubscribeLocalEvent<ActivatableUIRequiresPowerCellComponent, BoundUIClosedEvent>(OnBatteryClosed);

        SubscribeLocalEvent<PowerCellDrawComponent, EntRemovedFromContainerMessage>(OnPowerCellRemoved);
    }

    private void OnPowerCellRemoved(EntityUid uid, PowerCellDrawComponent component, EntRemovedFromContainerMessage args)
    {
        _cell.SetPowerCellDrawEnabled(uid, false);

        if (HasComp<ActivatableUIRequiresPowerCellComponent>(uid) &&
            TryComp<ActivatableUIComponent>(uid, out var activatable) &&
            activatable.Key != null)
        {
            _uiSystem.TryCloseAll(uid, activatable.Key);
        }
    }

    private void OnBatteryOpened(EntityUid uid, ActivatableUIRequiresPowerCellComponent component, BoundUIOpenedEvent args)
    {
        _cell.SetPowerCellDrawEnabled(uid, true);
    }

    private void OnBatteryClosed(EntityUid uid, ActivatableUIRequiresPowerCellComponent component, BoundUIClosedEvent args)
    {
        _cell.SetPowerCellDrawEnabled(uid, false);
    }

    /// <summary>
    /// Call if you want to check if the UI should close due to a recent battery usage.
    /// </summary>
    public void CheckUsage(EntityUid uid, ActivatableUIComponent? active = null, ActivatableUIRequiresPowerCellComponent? component = null, PowerCellDrawComponent? draw = null)
    {
        if (!Resolve(uid, ref component, ref draw, ref active, false) || active.Key == null)
            return;

        if (_cell.HasCharge(uid, draw.UseRate))
            return;

        _uiSystem.TryCloseAll(uid, active.Key);
    }

    private void OnBatteryOpenAttempt(EntityUid uid, ActivatableUIRequiresPowerCellComponent component, ActivatableUIOpenAttemptEvent args)
    {
        if (!TryComp<PowerCellDrawComponent>(uid, out var draw))
            return;

        // Check if we have the appropriate drawrate / userate to even open it.
        if (args.Cancelled || !_cell.HasCharge(uid, MathF.Max(draw.DrawRate, draw.UseRate), user: args.User))
        {
            args.Cancel();
            return;
        }
    }
}
