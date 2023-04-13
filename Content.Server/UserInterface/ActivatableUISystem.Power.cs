using Content.Server.PowerCell;

namespace Content.Server.UserInterface;

public sealed partial class ActivatableUISystem
{
    [Dependency] private readonly PowerCellSystem _cell = default!;

    private void InitializePower()
    {
        SubscribeLocalEvent<ActivatableUIPowerCellComponent, ActivatableUIOpenAttemptEvent>(OnBatteryOpenAttempt);
    }

    /// <summary>
    /// Call if you want to check if the UI should close due to a recent battery usage.
    /// </summary>
    public void CheckUsage(EntityUid uid, ActivatableUIComponent? active = null, ActivatableUIPowerCellComponent? component = null, PowerCellDrawComponent? draw = null)
    {
        if (!Resolve(uid, ref component, ref draw, ref active, false) || active.Key == null)
            return;

        if (_cell.HasCharge(uid, draw.UseRate))
            return;

        _uiSystem.TryCloseAll(uid, active.Key);
    }

    private void OnBatteryOpenAttempt(EntityUid uid, ActivatableUIPowerCellComponent component, ActivatableUIOpenAttemptEvent args)
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
