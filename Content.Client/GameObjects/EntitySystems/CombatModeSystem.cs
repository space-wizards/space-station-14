using Content.Client.GameObjects.Components.Mobs;
using Content.Client.UserInterface;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.EntitySystemMessages;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Input.Binding;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Players;

namespace Content.Client.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public sealed class CombatModeSystem : SharedCombatModeSystem
    {
        [Dependency] private readonly IGameHud _gameHud = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        public override void Initialize()
        {
            base.Initialize();

            _gameHud.OnCombatModeChanged = OnCombatModeChanged;
            _gameHud.OnTargetingZoneChanged = OnTargetingZoneChanged;

            CommandBinds.Builder
                .Bind(ContentKeyFunctions.ToggleCombatMode,
                    InputCmdHandler.FromDelegate(CombatModeToggled))
                .Register<CombatModeSystem>();
        }

        public override void Shutdown()
        {
            CommandBinds.Unregister<CombatModeSystem>();
            base.Shutdown();
        }

        private void CombatModeToggled(ICommonSession session)
        {
            if (_gameTiming.IsFirstTimePredicted)
            {
                EntityManager.RaisePredictiveEvent(
                    new CombatModeSystemMessages.SetCombatModeActiveMessage(!IsInCombatMode()));
            }
        }

        public bool IsInCombatMode()
        {
            var entity = _playerManager.LocalPlayer.ControlledEntity;
            if (entity == null || !entity.TryGetComponent(out CombatModeComponent combatMode))
            {
                return false;
            }

            return combatMode.IsInCombatMode;
        }

        private void OnTargetingZoneChanged(TargetingZone obj)
        {
            EntityManager.RaisePredictiveEvent(new CombatModeSystemMessages.SetTargetZoneMessage(obj));
        }

        private void OnCombatModeChanged(bool obj)
        {
            EntityManager.RaisePredictiveEvent(new CombatModeSystemMessages.SetCombatModeActiveMessage(obj));
        }
    }
}
