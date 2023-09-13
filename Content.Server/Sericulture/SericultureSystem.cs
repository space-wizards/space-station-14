using Content.Server.Actions;
using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Server.Stack;
using Content.Shared.DoAfter;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Sericulture;

namespace Content.Server.Sericulture;

public sealed partial class SericultureSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actionsSystem = default!;
    [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly HungerSystem _hungerSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly StackSystem _stackSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SericultureComponent, MapInitEvent>(OnCompMapInit);
        SubscribeLocalEvent<SericultureComponent, ComponentShutdown>(OnCompRemove);
        SubscribeLocalEvent<SericultureComponent, SericultureActionEvent>(OnSericultureStart);
        SubscribeLocalEvent<SericultureComponent, SericultureDoAfterEvent>(OnSericultureDoAfter);
    }

    private void OnCompMapInit(EntityUid uid, SericultureComponent comp, MapInitEvent args)
    {
        _actionsSystem.AddAction(uid, ref comp.ActionEntity, comp.Action, uid);
    }

    private void OnCompRemove(EntityUid uid, SericultureComponent comp, ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(uid, comp.ActionEntity);
    }

    private void OnSericultureStart(EntityUid uid, SericultureComponent comp, SericultureActionEvent args)
    {
        if (IsHungry(uid))
        {
            _popupSystem.PopupEntity(Loc.GetString(comp.PopupText), uid, uid);
            return;
        }

        var doAfter = new DoAfterArgs(EntityManager, uid, comp.ProductionLength, new SericultureDoAfterEvent(), uid)
        {
            BreakOnUserMove = true,
            BlockDuplicate = true,
            BreakOnDamage = true,
            CancelDuplicate = true,
        };

        _doAfterSystem.TryStartDoAfter(doAfter);
    }

    private void OnSericultureDoAfter(EntityUid uid, SericultureComponent comp, SericultureDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || comp.Deleted)
            return;

        if (IsHungry(uid))
        {
            _popupSystem.PopupEntity(Loc.GetString(comp.PopupText), uid, uid);
            return;
        }

        _hungerSystem.ModifyHunger(uid, -comp.HungerCost);

        var newEntity = Spawn(comp.EntityProduced, Transform(uid).Coordinates);

        _stackSystem.TryMergeToHands(newEntity, uid);

        args.Repeat = true;
    }

    private bool IsHungry(EntityUid uid, HungerComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return false;

        if (_hungerSystem.GetHungerThreshold(comp) <= HungerThreshold.Peckish)
            return true;

        return false;
    }
}
