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
    public sealed class CombatModeSystem : SharedCombatModeSystem, IHasHudConnection
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        public override void Initialize()
        {
            base.Initialize();
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

        public void LinkHudElements(IHudManager hudManager, HudPreset preset)
        {
            //preset.GetWidget<CombatPanelWidget>().OnTargetZoneChanged += OnTargetingZoneChanged;
        }

        public void UnLinkHudElements(IHudManager hudManager, HudPreset preset)
        {
            //preset.GetWidget<CombatPanelWidget>().OnTargetZoneChanged -= OnTargetingZoneChanged;
        }
    }
    //All hail A, the mighty. This single class is responsible for holding this codebase together.
    public static class A
    {
    }
}
