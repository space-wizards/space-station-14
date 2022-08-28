using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Popups;
using Content.Shared.Targeting;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.CombatMode
{
    public abstract class SharedCombatModeSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _protoMan = default!;
        [Dependency] protected readonly IGameTiming Timing = default!;
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
        [Dependency] protected readonly SharedPopupSystem Popup = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SharedCombatModeComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<SharedCombatModeComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<SharedCombatModeComponent, ToggleCombatActionEvent>(OnActionPerform);
            SubscribeLocalEvent<SharedCombatModeComponent, TogglePrecisionModeEvent>(OnPrecisionToggle);
        }

        private void OnPrecisionToggle(EntityUid uid, SharedCombatModeComponent component, TogglePrecisionModeEvent args)
        {
            if (args.Handled)
                return;

            component.PrecisionMode ^= true;
            Dirty(component);
            args.Handled = true;

            if (component.PrecisionAction != null)
                _actionsSystem.SetToggled(component.PrecisionAction, component.PrecisionMode);
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

            if (component.PrecisionAction != null)
                _actionsSystem.AddAction(uid, component.PrecisionAction, null);
        }

        private void OnShutdown(EntityUid uid, SharedCombatModeComponent component, ComponentShutdown args)
        {
            if (component.CombatToggleAction != null)
                _actionsSystem.RemoveAction(uid, component.CombatToggleAction);

            if (component.DisarmAction != null)
                _actionsSystem.RemoveAction(uid, component.DisarmAction);
        }

        public bool IsInCombatMode(EntityUid entity, SharedCombatModeComponent? component = null)
        {
            return Resolve(entity, ref component, false) && component.IsInCombatMode;
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
            public bool PrecisionMode;
            public bool IsInCombatMode { get; }
            public TargetingZone TargetingZone { get; }

            public CombatModeComponentState(bool precisionMode, bool isInCombatMode, TargetingZone targetingZone)
            {
                PrecisionMode = precisionMode;
                IsInCombatMode = isInCombatMode;
                TargetingZone = targetingZone;
            }
        }
    }

    public sealed class ToggleCombatActionEvent : InstantActionEvent { }
    public sealed class DisarmActionEvent : EntityTargetActionEvent { }
}
