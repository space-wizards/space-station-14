using Content.Shared.Species.Components;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Body.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;


namespace Content.Shared.Species;

public sealed partial class GibActionSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedBodySystem _bodySystem = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GibActionComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<GibActionComponent, GibActionEvent>(OnGibAction);
    }

    private void OnMobStateChanged(EntityUid uid, GibActionComponent comp, MobStateChangedEvent args)
    {
        if (!_protoManager.TryIndex<InstantActionPrototype>(comp.ActionPrototype, out var actionProto))
            return;

        if (!TryComp<MobStateComponent>(uid, out var MobState))
            return;

        foreach (var allowedState in comp.AllowedStates)
        {
            if(allowedState == MobState.CurrentState)
            {
                // The mob should never have more than 1 state so I don't see this being an issue
                var GibAction = new InstantAction(actionProto);
                _actionsSystem.AddAction(uid, GibAction, uid);
                return;
            }
        }

        _actionsSystem.RemoveAction(uid, new InstantAction(actionProto));
    }
    
    private void OnGibAction(EntityUid uid, GibActionComponent comp, GibActionEvent args)
    {
        _popupSystem.PopupClient(Loc.GetString(comp.PopupText, ("name", uid)), uid, uid);
        _bodySystem.GibBody(uid, true);
    }
       


    public sealed partial class GibActionEvent : InstantActionEvent { } 
}
