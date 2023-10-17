using Content.Server.Actions;
using Content.Shared.SS220.GhostRoleCast;

namespace Content.Server.SS220.GhostRoleCast;

public sealed class GhostRoleCastSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;

     public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GhostRoleCastComponent, ComponentStartup>(OnGhostRoleCastInit);
    }

    private void OnGhostRoleCastInit(EntityUid uid, GhostRoleCastComponent component, ComponentStartup args)
    {
        if (!component.ToggleGhostRoleNameAction.HasValue)
            _actions.AddAction(uid, ref component.ToggleGhostRoleNameAction, GhostRoleCastComponent.ToggleGhostRoleNameActionId);

        if (!component.ToggleGhostRoleCastAction.HasValue)
            _actions.AddAction(uid, ref component.ToggleGhostRoleCastAction, GhostRoleCastComponent.ToggleGhostRoleCastActionId);

        if (!component.ToggleGhostRoleRemoveAction.HasValue)
            _actions.AddAction(uid, ref component.ToggleGhostRoleRemoveAction, GhostRoleCastComponent.ToggleGhostRoleRemoveActionId);
    }

}
