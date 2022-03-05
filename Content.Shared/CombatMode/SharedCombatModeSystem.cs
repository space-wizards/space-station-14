using Content.Shared.Actions;

namespace Content.Shared.CombatMode
{
    public abstract class SharedCombatModeSystem : EntitySystem
    {
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<CombatModeSystemMessages.SetCombatModeActiveMessage>(CombatModeActiveHandler);
            SubscribeLocalEvent<CombatModeSystemMessages.SetCombatModeActiveMessage>(CombatModeActiveHandler);

            SubscribeLocalEvent<SharedCombatModeComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<SharedCombatModeComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<SharedCombatModeComponent, ToggleCombatActionEvent>(OnActionPerform);
        }

        private void OnStartup(EntityUid uid, SharedCombatModeComponent component, ComponentStartup args)
        {
            _actionsSystem.AddAction(uid, component.CombatToggleAction, null);
            _actionsSystem.AddAction(uid, component.DisarmAction, null);
        }

        private void OnShutdown(EntityUid uid, SharedCombatModeComponent component, ComponentShutdown args)
        {
            _actionsSystem.RemoveAction(uid, component.CombatToggleAction);
            _actionsSystem.RemoveAction(uid, component.DisarmAction);
        }

        private void OnActionPerform(EntityUid uid, SharedCombatModeComponent component, ToggleCombatActionEvent args)
        {
            if (args.Handled)
                return;

            component.IsInCombatMode = !component.IsInCombatMode;
            args.Handled = true;
        }

        private void CombatModeActiveHandler(CombatModeSystemMessages.SetCombatModeActiveMessage ev, EntitySessionEventArgs eventArgs)
        {
            var entity = eventArgs.SenderSession.AttachedEntity;

            if (entity == default || !EntityManager.TryGetComponent(entity, out SharedCombatModeComponent? combatModeComponent))
            {
                return;
            }

            combatModeComponent.IsInCombatMode = ev.Active;
        }
    }

    public sealed class ToggleCombatActionEvent : PerformActionEvent { }
    public sealed class DisarmActionEvent : PerformEntityTargetActionEvent { }
}
