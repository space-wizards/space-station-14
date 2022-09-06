using Content.Shared.ActionBlocker;
using Content.Shared.CombatMode;
using JetBrains.Annotations;
using Robust.Shared.GameStates;

namespace Content.Server.CombatMode
{
    [UsedImplicitly]
    public sealed class CombatModeSystem : SharedCombatModeSystem
    {
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SharedCombatModeComponent, DisarmActionEvent>(OnEntityActionPerform);
            SubscribeLocalEvent<SharedCombatModeComponent, ComponentGetState>(OnGetState);
        }

        private void OnGetState(EntityUid uid, SharedCombatModeComponent component, ref ComponentGetState args)
        {
            args.State = new CombatModeComponentState(component.IsInCombatMode, component.ActiveZone);
        }

        private void OnEntityActionPerform(EntityUid uid, SharedCombatModeComponent component, DisarmActionEvent args)
        {
            if (args.Handled)
                return;

            if (!_actionBlockerSystem.CanAttack(args.Performer))
                return;

            // TODO: Toggle disarm
        }
    }
}
