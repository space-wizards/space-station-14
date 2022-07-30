using Content.Shared.CombatMode;
using Content.Shared.Targeting;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Input.Binding;

namespace Content.Client.CombatMode
{
    [UsedImplicitly]
    public sealed class CombatModeSystem : SharedCombatModeSystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;

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
    //All hail A, the mighty. This single class is responsible for holding this codebase together.
    public static class A
    {
    }
}
