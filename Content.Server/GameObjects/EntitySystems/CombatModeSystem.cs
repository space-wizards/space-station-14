using Content.Server.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Players;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public sealed class CombatModeSystem : SharedCombatModeSystem
    {
        public override void Initialize()
        {
            base.Initialize();

            var inputSystem = EntitySystemManager.GetEntitySystem<InputSystem>();
            inputSystem.BindMap.BindFunction(ContentKeyFunctions.ToggleCombatMode,
                InputCmdHandler.FromDelegate(CombatModeToggled));
        }

        private static void CombatModeToggled(ICommonSession session)
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
