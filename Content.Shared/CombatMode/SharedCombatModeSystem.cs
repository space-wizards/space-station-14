using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.CombatMode
{
    public abstract class SharedCombatModeSystem : EntitySystem
    {
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
        [Dependency] private readonly IPrototypeManager _protoMan = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SharedCombatModeComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<SharedCombatModeComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<SharedCombatModeComponent, ToggleCombatActionEvent>(OnActionPerform);
        }

        private void OnStartup(EntityUid uid, SharedCombatModeComponent component, ComponentStartup args)
        {
            if (component.CombatToggleAction == null
                && _protoMan.TryIndex(component.CombatToggleActionId, out InstantActionPrototype? toggleProto))
            {
                component.CombatToggleAction = new(toggleProto);
            }

            if (component.CombatToggleAction != null)
                _actionsSystem.AddAction(uid, component.CombatToggleAction, null);

            if (component.DisarmAction == null
                && component.CanDisarm
                && _protoMan.TryIndex(component.DisarmActionId, out EntityTargetActionPrototype? disarmProto))
            {
                component.DisarmAction = new(disarmProto);
            }

            if (component.DisarmAction != null && component.CanDisarm)
                _actionsSystem.AddAction(uid, component.DisarmAction, null);
        }

        private void OnShutdown(EntityUid uid, SharedCombatModeComponent component, ComponentShutdown args)
        {
            if (component.CombatToggleAction != null)
                _actionsSystem.RemoveAction(uid, component.CombatToggleAction);

            if (component.DisarmAction != null)
                _actionsSystem.RemoveAction(uid, component.DisarmAction);
        }

        public bool IsInCombatMode(EntityUid entity)
        {
            return TryComp<SharedCombatModeComponent>(entity, out var combatMode) && combatMode.IsInCombatMode;
        }

        private void OnActionPerform(EntityUid uid, SharedCombatModeComponent component, ToggleCombatActionEvent args)
        {
            if (args.Handled)
                return;

            component.IsInCombatMode = !component.IsInCombatMode;
            args.Handled = true;
        }
    }

    public sealed class ToggleCombatActionEvent : InstantActionEvent { }
    public sealed class DisarmActionEvent : EntityTargetActionEvent { }
}
