using Content.Shared.Construction.EntitySystems;
using Content.Shared.Database;
using Content.Shared.Disposal.Components;

namespace Content.Shared.Disposal.Unit;

public abstract partial class SharedDisposalUnitSystem
{
    private void OnUiButtonPressed(Entity<DisposalUnitComponent> ent, ref DisposalUnitUiButtonPressedMessage args)
    {
        if (args.Actor is not { Valid: true } player)
            return;

        switch (args.Button)
        {
            case DisposalUnitUiButton.Eject:
                EjectContents(ent);
                _adminLog.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(player):player} hit eject button on {ToPrettyString(ent)}");
                break;
            case DisposalUnitUiButton.Engage:
                ToggleEngage(ent);
                _adminLog.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(player):player} hit flush button on {ToPrettyString(ent)}, it's now {(ent.Comp.Engaged ? "on" : "off")}");
                break;
            case DisposalUnitUiButton.Power:
                _power.TogglePower(ent.Owner, user: args.Actor);
                break;
            default:
                throw new ArgumentOutOfRangeException($"{ToPrettyString(player):player} attempted to hit a nonexistant button on {ToPrettyString(ent)}");
        }
    }

    /// <summary>
    /// Returns the estimated time when the disposal unit will be back to full pressure.
    /// </summary>
    /// <param name="ent">The disposal unit.</param>
    /// <returns>The estimated time.</returns>
    public TimeSpan EstimatedFullPressure(Entity<DisposalUnitComponent> ent)
    {
        if (ent.Comp.NextPressurized < _timing.CurTime)
            return TimeSpan.Zero;

        return ent.Comp.NextPressurized;
    }

    protected void UpdateUI(Entity<DisposalUnitComponent> entity)
    {
        if (_timing.ApplyingState)
            return;

        if (_ui.TryGetOpenUi(entity.Owner, DisposalUnitUiKey.Key, out var bui))
        {
            bui.Update<DisposalUnitBoundUserInterfaceState>();
        }
    }

    /// <summary>
    /// Updates the appearance data of a disposal unit.
    /// </summary>
    /// <param name="ent">The disposal unit.</param>
    protected void UpdateVisualState(Entity<DisposalUnitComponent> ent)
    {
        if (!TryComp(ent, out AppearanceComponent? appearance))
            return;

        var isAnchored = Transform(ent).Anchored;
        _appearance.SetData(ent, AnchorVisuals.Anchored, isAnchored, appearance);

        if (!isAnchored)
            return;

        var state = GetState(ent);
        _appearance.SetData(ent, DisposalUnitVisuals.IsReady, state == DisposalsPressureState.Ready, appearance);
        _appearance.SetData(ent, DisposalUnitVisuals.IsFlushing, state == DisposalsPressureState.Flushed, appearance);
        _appearance.SetData(ent, DisposalUnitVisuals.IsEngaged, ent.Comp.Engaged, appearance);

        if (!_power.IsPowered(ent.Owner))
            return;

        _appearance.SetData(ent, DisposalUnitVisuals.IsFull, GetContainedEntityCount(ent) > 0, appearance);
        _appearance.SetData(ent, DisposalUnitVisuals.IsCharging, state != DisposalsPressureState.Ready, appearance);
    }
}
