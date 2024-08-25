using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.PowerCell.Components;

namespace Content.Shared.PowerCell;

/// <summary>
/// Handles events to integrate PowerCellDraw with ItemToggle
/// </summary>
public sealed class ToggleCellDrawSystem : EntitySystem
{
    [Dependency] private readonly ItemToggleSystem _toggle = default!;
    [Dependency] private readonly SharedPowerCellSystem _cell = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ToggleCellDrawComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ToggleCellDrawComponent, ItemToggleActivateAttemptEvent>(OnActivateAttempt);
        SubscribeLocalEvent<ToggleCellDrawComponent, ItemToggledEvent>(OnToggled);
        SubscribeLocalEvent<ToggleCellDrawComponent, PowerCellSlotEmptyEvent>(OnEmpty);
    }

    private void OnMapInit(Entity<ToggleCellDrawComponent> ent, ref MapInitEvent args)
    {
        _cell.SetDrawEnabled(ent.Owner, _toggle.IsActivated(ent.Owner));
    }

    private void OnActivateAttempt(Entity<ToggleCellDrawComponent> ent, ref ItemToggleActivateAttemptEvent args)
    {
        if (!_cell.HasDrawCharge(ent, user: args.User)
            || !_cell.HasActivatableCharge(ent, user: args.User))
            args.Cancelled = true;
    }

    private void OnToggled(Entity<ToggleCellDrawComponent> ent, ref ItemToggledEvent args)
    {
        var uid = ent.Owner;
        var draw = Comp<PowerCellDrawComponent>(uid);
        _cell.QueueUpdate((uid, draw));
        _cell.SetDrawEnabled((uid, draw), args.Activated);
    }

    private void OnEmpty(Entity<ToggleCellDrawComponent> ent, ref PowerCellSlotEmptyEvent args)
    {
        _toggle.TryDeactivate(ent.Owner);
    }
}
