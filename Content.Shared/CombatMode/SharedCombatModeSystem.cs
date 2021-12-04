using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Shared.CombatMode
{
    public abstract class SharedCombatModeSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<CombatModeSystemMessages.SetCombatModeActiveMessage>(CombatModeActiveHandler);
            SubscribeLocalEvent<CombatModeSystemMessages.SetCombatModeActiveMessage>(CombatModeActiveHandler);
        }

        private void CombatModeActiveHandler(CombatModeSystemMessages.SetCombatModeActiveMessage ev, EntitySessionEventArgs eventArgs)
        {
            var entity = eventArgs.SenderSession.AttachedEntityUid;

            if (entity == null || !EntityManager.TryGetComponent(entity.Value, out SharedCombatModeComponent? combatModeComponent))
            {
                return;
            }

            combatModeComponent.IsInCombatMode = ev.Active;
        }
    }
}
