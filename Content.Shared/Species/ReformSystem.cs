using Content.Shared.Species.Components;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Mind;
using Content.Shared.Humanoid;
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
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReformComponent, ComponentInit>(OnCompInit);
        SubscribeLocalEvent<ReformComponent, ComponentShutdown>(OnCompRemove);

        SubscribeLocalEvent<ReformComponent, ReformEvent>(OnReform);
        SubscribeLocalEvent<ReformComponent, ReformDoAfterEvent>(OnDoAfter);
    }

    private void OnCompInit(EntityUid uid, ReformComponent comp, ComponentInit args)
    {
        // When the component is initialized, give them the action
        if (!_protoManager.TryIndex<InstantActionPrototype>(comp.ActionPrototype, out var actionProto))
            return;

        var reformAction = new InstantAction(actionProto);
        // See if the action should start with a delay, and give it that starting delay if so.
        if(comp.StartDelayed && reformAction.UseDelay != null)
            reformAction.Cooldown = (_gameTiming.CurTime, _gameTiming.CurTime + reformAction.UseDelay.Value);

        _actionsSystem.AddAction(uid, reformAction, uid);
    }

    private void OnCompRemove(EntityUid uid, ReformComponent comp, ComponentShutdown args)
    {
        // Remove the action if they have it and the component is removed
        if (!_protoManager.TryIndex<InstantActionPrototype>(comp.ActionPrototype, out var actionProto))
            return;

        _actionsSystem.RemoveAction(uid, new InstantAction(actionProto));
    }

    private void OnReform(EntityUid uid, ReformComponent comp, ReformEvent args)
    {
        // Stun them when they use the action for the amount of reform time.
        _stunSystem.TryStun(uid, TimeSpan.FromSeconds(comp.ReformTime), true);
        _popupSystem.PopupClient(Loc.GetString(comp.PopupText, ("name", uid)), uid, uid);

        // Create a doafter & start it
        var doAfter = new DoAfterArgs(uid, comp.ReformTime, new ReformDoAfterEvent(), uid)
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
        var child = Spawn(comp.Prototype, Transform(uid).Coordinates);

        // If the first entity has appearanceinfo, replace the new mobs' appearanace and name with that. It will be random otherwise.
        if(TryComp<AppearanceInfoComponent>(uid, out var appearance))
        {
            RemComp<HumanoidAppearanceComponent>(child);
            AddComp(child, appearance.Appearance);

            _metaData.SetEntityName(child, appearance.Name);
        }

        // This transfers the mind to the new entity
        if (_mindSystem.TryGetMind(uid, out var mindId, out var mind))
                _mindSystem.TransferTo(mindId, child, mind: mind);

        // Deleted the old entity
        QueueDel(uid);
    }

    public sealed partial class ReformEvent : InstantActionEvent { } 
    
    [Serializable, NetSerializable]
    public sealed partial class ReformDoAfterEvent : SimpleDoAfterEvent { }
}
