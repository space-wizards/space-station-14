using Content.Shared.Actions;
using Content.Shared.Interaction.Events;

namespace Content.Shared.CombatMode.Pacification
{
    public sealed class PacificationSystem : EntitySystem
    {
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PacifiedComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<PacifiedComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<PacifiedComponent, AttackAttemptEvent>(OnAttackAttempt);
        }

        private void OnAttackAttempt(EntityUid uid, PacifiedComponent component, AttackAttemptEvent args)
        {
            args.Cancel();
        }

        private void OnStartup(EntityUid uid, PacifiedComponent component, ComponentStartup args)
        {
            if (!TryComp<SharedCombatModeComponent>(uid, out var combatMode))
                return;

            if (combatMode.CanDisarm != null)
                combatMode.CanDisarm = false;

            if (combatMode.CombatToggleAction != null)
            {
                combatMode.IsInCombatMode = false;
                _actionsSystem.SetEnabled(combatMode.CombatToggleAction, false);
            }
        }

        private void OnShutdown(EntityUid uid, PacifiedComponent component, ComponentShutdown args)
        {
            if (!TryComp<SharedCombatModeComponent>(uid, out var combatMode))
                return;

            if (combatMode.CanDisarm != null)
                combatMode.CanDisarm = true;

            if (combatMode.CombatToggleAction != null)
                _actionsSystem.SetEnabled(combatMode.CombatToggleAction, true);
        }
    }
}
