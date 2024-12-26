using Content.Shared.Species.Components;
using Content.Shared.Actions;
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
        // When the mob changes state, check if they're dead and give them the action if so. 
        if (!TryComp<MobStateComponent>(uid, out var mobState))
            return;

        if (!_protoManager.TryIndex<EntityPrototype>(comp.ActionPrototype, out var actionProto))
            return;


        foreach (var allowedState in comp.AllowedStates)
        {
            if(allowedState == mobState.CurrentState)
            {
                // The mob should never have more than 1 state so I don't see this being an issue
                _actionsSystem.AddAction(uid, ref comp.ActionEntity, comp.ActionPrototype);
                return;
            }
        }

        // If they aren't given the action, remove it.
        _actionsSystem.RemoveAction(uid, comp.ActionEntity);
    }
    
    private void OnGibAction(EntityUid uid, GibActionComponent comp, GibActionEvent args)
    {
        // When they use the action, gib them.
        _popupSystem.PopupClient(Loc.GetString(comp.PopupText, ("name", uid)), uid, uid);
        _bodySystem.GibBody(uid, true);
    }
       


    public sealed partial class GibActionEvent : InstantActionEvent { } 
}
