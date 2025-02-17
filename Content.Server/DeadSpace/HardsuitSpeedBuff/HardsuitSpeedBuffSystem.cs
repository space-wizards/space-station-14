// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Server.Actions;
using Content.Shared.DeadSpace.HardsuitSpeedBuff;
using Content.Shared.PowerCell;
using Content.Shared.Clothing;
using Content.Shared.Movement.Systems;
using Content.Shared.Inventory;
using Content.Shared.Actions;
using Content.Server.PowerCell;

namespace Content.Server.DeadSpace.HardsuitSpeedBuff;

public sealed class HardsuitSpeedBuffSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly PowerCellSystem _cell = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<HardsuitSpeedBuffComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<HardsuitSpeedBuffComponent, ActivateSpeedBuffActionEvent>(OnSpeedBuffActivate);
        SubscribeLocalEvent<HardsuitSpeedBuffComponent, InventoryRelayedEvent<RefreshMovementSpeedModifiersEvent>>(OnRefreshMovementSpeedModifiers);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<HardsuitSpeedBuffComponent, PowerCellDrawComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var comp, out _, out var xform))
        {
            if (!_cell.HasCharge(uid, 10))
            {
                comp.Activated = false;
                _movement.RefreshMovementSpeedModifiers(xform.ParentUid);
            }
        }
    }

    private void OnGetActions(EntityUid uid, HardsuitSpeedBuffComponent comp, GetItemActionsEvent args)
    {
        args.AddAction(ref comp.ActionEntity, comp.Action);
    }

    private void OnSpeedBuffActivate(Entity<HardsuitSpeedBuffComponent> ent, ref ActivateSpeedBuffActionEvent args)
    {
        if (!TryComp<PowerCellDrawComponent>(ent.Owner, out var powerCell))
            return;

        if (!TryComp<ClothingSpeedModifierComponent>(ent.Owner, out _))
            return;

        if (powerCell.Enabled)
        {
            powerCell.Enabled = false;
            ent.Comp.Activated = false;
            _movement.RefreshMovementSpeedModifiers(args.Performer);
        }
        else
        {
            powerCell.Enabled = true;
            ent.Comp.Activated = true;
            _movement.RefreshMovementSpeedModifiers(args.Performer);
        }
    }

    private void OnRefreshMovementSpeedModifiers(EntityUid uid, HardsuitSpeedBuffComponent comp, InventoryRelayedEvent<RefreshMovementSpeedModifiersEvent> args)
    {
        if (comp.Activated)
            args.Args.ModifySpeed(comp.WalkModifier, comp.SprintModifier);
    }
}
