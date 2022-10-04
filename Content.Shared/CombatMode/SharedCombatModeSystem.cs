using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Targeting;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.CombatMode
{
    public abstract class SharedCombatModeSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _protoMan = default!;
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;

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
        }

        private void OnShutdown(EntityUid uid, SharedCombatModeComponent component, ComponentShutdown args)
        {
            if (component.CombatToggleAction != null)
                _actionsSystem.RemoveAction(uid, component.CombatToggleAction);
        }

        public bool IsInCombatMode(EntityUid? entity, SharedCombatModeComponent? component = null)
        {
            return entity != null && Resolve(entity.Value, ref component, false) && component.IsInCombatMode;
        }

        private void OnActionPerform(EntityUid uid, SharedCombatModeComponent component, ToggleCombatActionEvent args)
        {
            if (args.Handled)
                return;

            component.IsInCombatMode = !component.IsInCombatMode;
            args.Handled = true;
        }

        [Serializable, NetSerializable]
        protected sealed class CombatModeComponentState : ComponentState
        {
            public bool IsInCombatMode { get; }
            public TargetingZone TargetingZone { get; }

            public CombatModeComponentState(bool isInCombatMode, TargetingZone targetingZone)
            {
                IsInCombatMode = isInCombatMode;
                TargetingZone = targetingZone;
            }
        }
    }

    public sealed class ToggleCombatActionEvent : InstantActionEvent { }
}
