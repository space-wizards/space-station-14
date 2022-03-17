using Content.Client.CombatMode.UI;
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
        [Dependency] private readonly IHudManager _gameHud = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        private CombatPanelWidget? _combatPanel;
        public override void Initialize()
        {
            base.Initialize();
            _combatPanel = _gameHud.GetUIWidget<CombatPanelWidget>();
            if (_combatPanel == null) throw  new SystemException("Combat panel not found, cannot setup targeting!");
            _combatPanel.OnTargetZoneChanged += OnTargetingZoneChanged;

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
            return EntityManager.TryGetComponent(_playerManager.LocalPlayer?.ControlledEntity, out CombatModeComponent? combatMode) &&
                   combatMode.IsInCombatMode;
        }

        private void OnTargetingZoneChanged(TargetingZone obj)
        {
            EntityManager.RaisePredictiveEvent(new CombatModeSystemMessages.SetTargetZoneMessage(obj));
        }
    }

    public static class A
    {
    }
}
