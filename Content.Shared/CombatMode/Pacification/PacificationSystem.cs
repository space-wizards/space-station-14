using Content.Shared.CombatMode;
using Content.Shared.Actions;

namespace Content.Shared.CombatMode.Pacification
{
    public sealed class PacificationSystem : EntitySystem
    {
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PacifiedComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<PacifiedComponent, ComponentShutdown>(OnShutdown);
        }

        private void OnInit(EntityUid uid, PacifiedComponent component, ComponentInit args)
        {
            if (!TryComp<SharedCombatModeComponent>(uid, out var combatMode))
                return;

            if (combatMode.DisarmAction != null)
            {
                _actionsSystem.SetToggled(combatMode.DisarmAction, false);
                _actionsSystem.SetEnabled(combatMode.DisarmAction, false);
            }
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

            if (combatMode.DisarmAction != null)
                _actionsSystem.SetEnabled(combatMode.DisarmAction, true);
            if (combatMode.CombatToggleAction != null)
                _actionsSystem.SetEnabled(combatMode.CombatToggleAction, true);
        }
    }
}
