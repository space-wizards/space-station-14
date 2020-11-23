using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.EntitySystemMessages;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;

namespace Content.Shared.GameObjects.EntitySystems
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
            var entity = eventArgs.SenderSession?.AttachedEntity;

            if (entity == null || !entity.TryGetComponent(out SharedCombatModeComponent combatModeComponent))
            {
                return;
            }

            combatModeComponent.IsInCombatMode = ev.Active;
        }
    }
}
