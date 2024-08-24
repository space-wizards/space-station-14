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

        SubscribeLocalEvent<ToggleCellDrawComponent, ItemToggleActivateAttemptEvent>(OnActivateAttempt);
        SubscribeLocalEvent<ToggleCellDrawComponent, ItemToggledEvent>(OnToggled);
        SubscribeLocalEvent<ToggleCellDrawComponent, PowerCellSlotEmptyEvent>(OnEmpty);
    }

    private void OnActivateAttempt(Entity<ToggleCellDrawComponent> ent, ref ItemToggleActivateAttemptEvent args)
    {
        if (!_cell.HasDrawCharge(ent, ent.Comp, user: args.User)
            || !_cell.HasActivatableCharge(ent, ent.Comp, user: args.User))
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
