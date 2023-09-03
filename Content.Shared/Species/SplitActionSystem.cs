using Content.Shared.Species.Components;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Timing;
using Content.Shared.DoAfter;
using Content.Shared.Damage;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared.Species;

public sealed partial class SplitActionSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SplitActionComponent, ComponentInit>(OnCompInit);
        SubscribeLocalEvent<SplitActionComponent, ComponentShutdown>(OnCompRemove);

        SubscribeLocalEvent<SplitActionComponent, SplitActionEvent>(OnSplitAction);
        SubscribeLocalEvent<SplitActionComponent, SplitActionDoAfterEvent>(OnDoAfter);
    }

    private void OnCompInit(EntityUid uid, SplitActionComponent comp, ComponentInit args)
    {
        // When the component is initialized, give them the action
        if (!_protoManager.TryIndex<InstantActionPrototype>(comp.ActionPrototype, out var actionProto))
            return;

        var splitAction = new InstantAction(actionProto);
        // See if the action should start with a delay, and give it that starting delay if so.
        if(comp.StartDelayed && splitAction.UseDelay != null)
            splitAction.Cooldown = (_gameTiming.CurTime, _gameTiming.CurTime + splitAction.UseDelay.Value);

        _actionsSystem.AddAction(uid, splitAction, uid);
    }

    private void OnCompRemove(EntityUid uid, SplitActionComponent comp, ComponentShutdown args)
    {
        // Remove the action if they have it and the component is removed
        if (!_protoManager.TryIndex<InstantActionPrototype>(comp.ActionPrototype, out var actionProto))
            return;

        _actionsSystem.RemoveAction(uid, new InstantAction(actionProto));
    }

    private void OnSplitAction(EntityUid uid, SplitActionComponent comp, SplitActionEvent args)
    {
        // TODO: Make it cancel if you don't have enough health.
        // Stun them when they use the action for the specified time. This is to make sure they know they can't move.
        _stunSystem.TryParalyze(uid, comp.StunTime, true);
        _popupSystem.PopupClient(Loc.GetString(comp.PopupText), uid, uid);

        // Create a doafter & start it
        var doAfter = new DoAfterArgs(uid, comp.SplitTime, new SplitActionDoAfterEvent(), uid)
        {
            BreakOnUserMove = true,
            BlockDuplicate = true,
            BreakOnDamage = false,
            CancelDuplicate = true,
            RequireCanInteract = false,
        };

        _doAfterSystem.TryStartDoAfter(doAfter);
        args.Handled = true;
    }

    private void OnDoAfter(EntityUid uid, SplitActionComponent comp, SplitActionDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || comp.Deleted)
            return;

        // Stun them for the specified time, then damage them for the specified amount
        _stunSystem.TryParalyze(uid, comp.StunTime, true);
        _damageableSystem.TryChangeDamage(uid, comp.Damage, true);

        // Spawn the entity (serverside)
        if(_net.IsServer)
            Spawn(comp.EntityProduced, Transform(uid).Coordinates);
    }

    public sealed partial class SplitActionEvent : InstantActionEvent { } 
    
    [Serializable, NetSerializable]
    public sealed partial class SplitActionDoAfterEvent : SimpleDoAfterEvent { }
}
