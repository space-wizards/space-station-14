using Content.Shared.CombatMode;
using Content.Shared.Targeting;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Shared.GameStates;
using Robust.Shared.Input.Binding;

namespace Content.Client.CombatMode
{
    [UsedImplicitly]
    public sealed class CombatModeSystem : SharedCombatModeSystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SharedCombatModeComponent, ComponentHandleState>(OnHandleState);
        }

        private void OnHandleState(EntityUid uid, SharedCombatModeComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not CombatModeComponentState state)
                return;

            component.IsInCombatMode = state.IsInCombatMode;
            component.ActiveZone = state.TargetingZone;
        }
        public override void Shutdown()
        {
            CommandBinds.Unregister<CombatModeSystem>();
            base.Shutdown();
        }

        public bool IsInCombatMode()
        {
            var entity = _playerManager.LocalPlayer?.ControlledEntity;

            if (entity == null)
                return false;

            return IsInCombatMode(entity.Value);
        }

        private void OnTargetingZoneChanged(TargetingZone obj)
        {
            EntityManager.RaisePredictiveEvent(new CombatModeSystemMessages.SetTargetZoneMessage(obj));
        }
    }
}
