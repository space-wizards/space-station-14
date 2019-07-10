using Content.Server.GameObjects.Components.Mobs;
using Content.Shared.Input;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Input;
using Robust.Shared.Players;

namespace Content.Server.GameObjects.EntitySystems
{
    public sealed class CombatModeSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            var inputSystem = EntitySystemManager.GetEntitySystem<InputSystem>();
            inputSystem.BindMap.BindFunction(ContentKeyFunctions.ToggleCombatMode,
                InputCmdHandler.FromDelegate(CombatModeToggled));
        }

        private void CombatModeToggled(ICommonSession session)
        {
            var playerSession = (IPlayerSession) session;

            if (playerSession.AttachedEntity == null ||
                !playerSession.AttachedEntity.TryGetComponent(out CombatModeComponent combatModeComponent))
            {
                return;
            }

            combatModeComponent.IsInCombatMode = !combatModeComponent.IsInCombatMode;
        }
    }
}
