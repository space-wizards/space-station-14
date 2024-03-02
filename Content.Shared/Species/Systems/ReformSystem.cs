using Content.Shared.Species.Components;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Mind;
using Content.Shared.Zombies;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Species;

public sealed partial class ReformSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly INetManager _netMan = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReformComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ReformComponent, ComponentShutdown>(OnCompRemove);

        SubscribeLocalEvent<ReformComponent, ReformEvent>(OnReform);
        SubscribeLocalEvent<ReformComponent, ReformDoAfterEvent>(OnDoAfter);

        SubscribeLocalEvent<ReformComponent, EntityZombifiedEvent>(OnZombified);
    }

    private void OnMapInit(EntityUid uid, ReformComponent comp, MapInitEvent args)
    {
        // When the map is initialized, give them the action
        if (comp.ActionPrototype != default && !_protoManager.TryIndex<EntityPrototype>(comp.ActionPrototype, out var actionProto))
            return;

        _actionsSystem.AddAction(uid, ref comp.ActionEntity, out var reformAction, comp.ActionPrototype);

        // See if the action should start with a delay, and give it that starting delay if so.
        if (comp.StartDelayed && reformAction != null && reformAction.UseDelay != null)
        {
            var start = _gameTiming.CurTime;
            var end = _gameTiming.CurTime + reformAction.UseDelay.Value;

            _actionsSystem.SetCooldown(comp.ActionEntity!.Value, start, end);
        }
    }

    private void OnCompRemove(EntityUid uid, ReformComponent comp, ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(uid, comp.ActionEntity);
    }

    private void OnReform(EntityUid uid, ReformComponent comp, ReformEvent args)
    {
        // Stun them when they use the action for the amount of reform time.
        if (comp.ShouldStun)
            _stunSystem.TryStun(uid, TimeSpan.FromSeconds(comp.ReformTime), true);
        _popupSystem.PopupClient(Loc.GetString(comp.PopupText, ("name", uid)), uid, uid);

        // Create a doafter & start it
        var doAfter = new DoAfterArgs(EntityManager, uid, comp.ReformTime, new ReformDoAfterEvent(), uid)
        {
            BreakOnUserMove = true,
            BlockDuplicate = true,
            BreakOnDamage = true,
            CancelDuplicate = true,
            RequireCanInteract = false,
        };

        _doAfterSystem.TryStartDoAfter(doAfter);
        args.Handled = true;
    }

    private void OnDoAfter(EntityUid uid, ReformComponent comp, ReformDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || comp.Deleted)
            return;

        if (_netMan.IsClient)
            return;

        // Spawn a new entity
        // This is, to an extent, taken from polymorph. I don't use polymorph for various reasons- most notably that this is permanent. 
        var child = Spawn(comp.ReformPrototype, Transform(uid).Coordinates);

        // This transfers the mind to the new entity
        if (_mindSystem.TryGetMind(uid, out var mindId, out var mind))
            _mindSystem.TransferTo(mindId, child, mind: mind);

        // Delete the old entity
        QueueDel(uid);
    }

    private void OnZombified(EntityUid uid, ReformComponent comp, ref EntityZombifiedEvent args)
    {
        _actionsSystem.RemoveAction(uid, comp.ActionEntity); // Zombies can't reform
    }

    public sealed partial class ReformEvent : InstantActionEvent { } 
    
    [Serializable, NetSerializable]
    public sealed partial class ReformDoAfterEvent : SimpleDoAfterEvent { }
}
