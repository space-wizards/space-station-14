using Content.Client.HUD;
using Content.Shared.CombatMode;
using Content.Shared.Targeting;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Input.Binding;
using Robust.Shared.IoC;

namespace Content.Client.CombatMode
{
    [UsedImplicitly]
    public sealed class CombatModeSystem : SharedCombatModeSystem
    {
        [Dependency] private readonly IGameHud _gameHud = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        public override void Initialize()
        {
            base.Initialize();

            _gameHud.OnTargetingZoneChanged = OnTargetingZoneChanged;

            SubscribeLocalEvent<CombatModeComponent, PlayerAttachedEvent>((_, component, _) => component.PlayerAttached());
            SubscribeLocalEvent<CombatModeComponent, PlayerDetachedEvent>((_, component, _) => component.PlayerDetached());
        }

        public override void Shutdown()
        {
            CommandBinds.Unregister<CombatModeSystem>();
            base.Shutdown();
        }

        public bool IsInCombatMode()
        {
            var entity = _playerManager.LocalPlayer?.ControlledEntity;
            if (entity == null || !entity.TryGetComponent(out CombatModeComponent? combatMode))
            {
                return false;
            }

            return combatMode.IsInCombatMode;
        }

        private void OnTargetingZoneChanged(TargetingZone obj)
        {
            EntityManager.RaisePredictiveEvent(new CombatModeSystemMessages.SetTargetZoneMessage(obj));
        }
    }
}
