using Content.Server.GameObjects.Components.Mobs;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Input;
using Robust.Shared.Interfaces.Network;
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

            var inputSystem = EntitySystemManager.GetEntitySystem<InputSystem>();
            inputSystem.BindMap.BindFunction(ContentKeyFunctions.ToggleCombatMode,
                InputCmdHandler.FromDelegate(CombatModeToggled));
        }

        public override void RegisterMessageTypes()
        {
            base.RegisterMessageTypes();

            RegisterMessageType<SetTargetZoneMessage>();
            RegisterMessageType<SetCombatModeActiveMessage>();
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

        public override void HandleNetMessage(INetChannel channel, EntitySystemMessage message)
        {
            base.HandleNetMessage(channel, message);

            var player = _playerManager.GetSessionByChannel(channel);
            if (player.AttachedEntity == null
                || !player.AttachedEntity.TryGetComponent(out CombatModeComponent combatModeComponent))
            {
                return;
            }

            switch (message)
            {
                case SetTargetZoneMessage setTargetZone:
                    combatModeComponent.ActiveZone = setTargetZone.TargetZone;
                    break;

                case SetCombatModeActiveMessage setActive:
                    combatModeComponent.IsInCombatMode = setActive.Active;
                    break;
            }
        }
    }
}
