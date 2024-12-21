using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Content.Shared.Xenobiology;
using Content.Shared.Xenobiology.Components.Container;
using Content.Shared.Xenobiology.Components.Tools;
using Content.Shared.Xenobiology.Systems;
using Content.Shared.Xenobiology.Visuals;
using Robust.Server.GameObjects;

namespace Content.Server.Xenobiology;

public sealed class CellSystem : SharedCellSystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CellCollectorComponent, BeforeRangedInteractEvent>(OnCollectorInteract);
        SubscribeLocalEvent<CellCollectorComponent, CellCollectorDoAfter>(OnCollectorCollectDoAfter);
    }

    private void OnCollectorInteract(Entity<CellCollectorComponent> ent, ref BeforeRangedInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is null)
            return;

        if (!TryComp<CellContainerComponent>(args.Target, out var containerComponent))
            return;

        var direction = containerComponent.Empty
            ? CellCollectorDirection.Transfer
            : CellCollectorDirection.Collection;

        if (!CollectorInteractValidate(ent, (args.Target.Value, containerComponent), direction))
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, ent.Comp.Delay, new CellCollectorDoAfter(direction), ent, target: args.Target, used: ent)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            MovementThreshold = 0.5f,
            BlockDuplicate = true,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        args.Handled = true;
    }

    private void OnCollectorCollectDoAfter(Entity<CellCollectorComponent> ent, ref CellCollectorDoAfter args)
    {
        if (args.Handled || args.Cancelled || args.Target is null)
            return;

        if (!CollectorInteractValidate(ent, args.Target.Value, args.Direction))
            return;

        switch (args.Direction)
        {
            case CellCollectorDirection.Collection:
                CopyCells(ent.Owner, args.Target.Value);

                _popup.PopupPredicted(Loc.GetString("cell-collector-collected"), ent, null);

                if (ent.Comp.Damage is not null)
                    _damageable.TryChangeDamage(args.Target.Value, ent.Comp.Damage);

                ent.Comp.Usages--;
                break;

            case CellCollectorDirection.Transfer:
                CopyCells(args.Target.Value, ent.Owner);
                ClearCells(ent.Owner);

                _popup.PopupPredicted(Loc.GetString("cell-collector-transfer"), ent, null);
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }

        UpdateCollectorAppearance(ent);
        args.Handled = true;
    }

    private void UpdateCollectorAppearance(Entity<CellCollectorComponent, CellContainerComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp2))
            return;

        _appearance.SetData(ent, CellCollectorVisuals.State, ent.Comp2.Empty);
    }

    private bool CollectorInteractValidate(Entity<CellCollectorComponent, CellContainerComponent?> ent,
        Entity<CellContainerComponent?> target,
        CellCollectorDirection direction,
        bool popup = true)
    {
        if (!Resolve(ent, ref ent.Comp2) || !Resolve(target, ref target.Comp))
            return false;

        switch (direction)
        {
            case CellCollectorDirection.Collection:
                if (!ent.Comp2.Empty)
                {
                    if (!popup)
                        return false;

                    _popup.PopupPredicted(Loc.GetString("cell-collector-full"), ent, null, PopupType.SmallCaution);
                    return false;
                }

                if (ent.Comp1.Usages == 0)
                {
                    if (!popup)
                        return false;

                    _popup.PopupPredicted(Loc.GetString("cell-collector-already-used"), ent, null, PopupType.SmallCaution);
                    return false;
                }

                if (!target.Comp.AllowCollection)
                {
                    if (!popup)
                        return false;

                    _popup.PopupPredicted(Loc.GetString("cell-collector-target-cant-collected"), ent, null, PopupType.SmallCaution);
                    return false;
                }
                break;

            case CellCollectorDirection.Transfer:
                if (_entityWhitelist.IsWhitelistFail(target.Comp.ToolsTransferWhitelist, ent) ||
                    target.Comp.ToolsTransferWhitelist is null ||
                    !target.Comp.AllowTransfer)
                    return false;

                if (ent.Comp2.Empty)
                {
                    if (!popup)
                        return false;

                    _popup.PopupPredicted(Loc.GetString("cell-collector-empty"), ent, null, PopupType.SmallCaution);
                    return false;
                }
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
        }

        return true;
    }
}
