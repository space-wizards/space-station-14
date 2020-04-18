using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.EntitySystemMessages;
using Content.Shared.Interfaces;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Random;
using Logger = Robust.Shared.Log.Logger;

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
            var entity = eventArgs.SenderSession.AttachedEntity;

            if (!entity.TryGetComponent(out SharedCombatModeComponent combatModeComponent))
            {
                return;
            }

            combatModeComponent.IsInCombatMode = ev.Active;
        }
    }
}
