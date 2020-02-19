using Content.Server.GameObjects.Components.Mobs;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Players;
using static Content.Shared.GameObjects.EntitySystemMessages.CombatModeSystemMessages;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public sealed class CombatModeSystem : EntitySystem
    {
#pragma warning disable 649
        [Dependency] private readonly IPlayerManager _playerManager;
#pragma warning restore 649

        public override void Initialize()
        {
            base.Initialize();

            SubscribeEvent<SetTargetZoneMessage>(SetTargetZoneHandler);
            SubscribeEvent<SetCombatModeActiveMessage>(SetCombatModeActiveHandler);

            var inputSystem = EntitySystemManager.GetEntitySystem<InputSystem>();
            inputSystem.BindMap.BindFunction(ContentKeyFunctions.ToggleCombatMode,
                InputCmdHandler.FromDelegate(CombatModeToggled));
        }

        private void SetCombatModeActiveHandler(SetCombatModeActiveMessage ev)
        {
            if (!TryGetCombatComponent(ev, out var combatModeComponent))
                return;

            combatModeComponent.IsInCombatMode = ev.Active;
        }

        private void SetTargetZoneHandler(SetTargetZoneMessage ev)
        {
            if (!TryGetCombatComponent(ev, out var combatModeComponent))
                return;

            combatModeComponent.ActiveZone = ev.TargetZone;
        }

        private bool TryGetCombatComponent(EntitySystemMessage ev, out CombatModeComponent combatModeComponent)
        {
            if (ev.NetChannel == null)
            {
                combatModeComponent = default;
                return false;
            }

            var player = _playerManager.GetSessionByChannel(ev.NetChannel);
            if (player.AttachedEntity != null && player.AttachedEntity.TryGetComponent(out combatModeComponent))
                return true;

            combatModeComponent = default;
            return false;

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
