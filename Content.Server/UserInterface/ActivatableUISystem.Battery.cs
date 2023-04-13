using Content.Server.PowerCell;
using Content.Shared.PowerCell.Components;
using Content.Shared.UserInterface;

namespace Content.Server.UserInterface;

public sealed partial class ActivatableUISystem
{
    [Dependency] private readonly PowerCellSystem _cell = default!;

    private void InitializeBattery()
    {
        SubscribeLocalEvent<ActivatableUIBatteryComponent, ActivatableUIOpenAttemptEvent>(OnBatteryOpenAttempt);
    }

    /// <summary>
    /// Call if you want to check if the UI should close due to a recent battery usage.
    /// </summary>
    public void CheckUsage(EntityUid uid, ActivatableUIBatteryComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (_cell.HasCharge(uid, component.UseRate))
            return;

        _uiSystem.TryCloseAll(uid, component.UiKey);
    }

    private void OnBatteryOpenAttempt(EntityUid uid, ActivatableUIBatteryComponent component, ActivatableUIOpenAttemptEvent args)
    {
        // Check if we have the appropriate drawrate / userate to even open it.
        if (args.Cancelled || !_cell.HasCharge(uid, MathF.Max(component.DrawRate, component.UseRate), user: args.User))
        {
            args.Cancel();
            return;
        }
    }

    private void UpdateBattery(float frameTime)
    {
        var query = EntityQueryEnumerator<ActivatableUIBatteryComponent, PowerCellSlotComponent>();

        while (query.MoveNext(out var uid, out var comp, out var slot))
        {
            if (!_uiSystem.IsUiOpen(uid, comp.UiKey))
                continue;

            if (!_cell.TryGetBatteryFromSlot(uid, out var battery, slot))
                continue;

            if (battery.TryUseCharge(comp.DrawRate * frameTime))
                continue;

            _uiSystem.TryCloseAll(uid, comp.UiKey);
        }
    }
}
