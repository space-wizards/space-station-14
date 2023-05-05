using Content.Shared.CombatMode;
using Content.Shared.Targeting;
using Content.Shared.CCVar;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.Input;
using Robust.Client.Graphics;
using Robust.Shared.Input.Binding;
using Robust.Shared.GameStates;
using Robust.Shared.Configuration;

namespace Content.Client.CombatMode
{
    [UsedImplicitly]
    public sealed class CombatModeSystem : SharedCombatModeSystem
    {
        [Dependency] private readonly IOverlayManager _overlayManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IEyeManager _eye = default!;
        public event Action? LocalPlayerCombatModeUpdated;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<CombatModeComponent, ComponentHandleState>(OnHandleState);

            OnShowCombatIndicatorsChanged(_cfg.GetCVar(CCVars.HudHeldItemShow));
            _cfg.OnValueChanged(CCVars.HudHeldItemShow, OnShowCombatIndicatorsChanged, true);
        }

        private void OnHandleState(EntityUid uid, CombatModeComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not CombatModeComponentState state)
                return;

            component.IsInCombatMode = state.IsInCombatMode;
            component.ActiveZone = state.TargetingZone;
            UpdateHud(uid);
        }

        public override void Shutdown()
        {
            CommandBinds.Unregister<CombatModeSystem>();

            _overlayManager.RemoveOverlay<ShowCombatModeIndicatorsOverlay>();

            base.Shutdown();
        }

        private void OnTargetingZoneChanged(TargetingZone obj)
        {
            EntityManager.RaisePredictiveEvent(new CombatModeSystemMessages.SetTargetZoneMessage(obj));
        }

        public bool IsInCombatMode()
        {
            var entity = _playerManager.LocalPlayer?.ControlledEntity;

            if (entity == null)
                return false;

            return IsInCombatMode(entity.Value);
        }

        public override void SetInCombatMode(EntityUid entity, bool inCombatMode, CombatModeComponent? component = null)
        {
            base.SetInCombatMode(entity, inCombatMode, component);
            UpdateHud(entity);
        }

        public override void SetActiveZone(EntityUid entity, TargetingZone zone, CombatModeComponent? component = null)
        {
            base.SetActiveZone(entity, zone, component);
            UpdateHud(entity);
        }

        private void UpdateHud(EntityUid entity)
        {
            if (entity != _playerManager.LocalPlayer?.ControlledEntity)
            {
                return;
            }

            LocalPlayerCombatModeUpdated?.Invoke();
        }

        private void OnShowCombatIndicatorsChanged(bool isShow)
        {
            if (isShow)
                AddCombatModeIndicatorsOverlay();
            else
                _overlayManager.RemoveOverlay<ShowCombatModeIndicatorsOverlay>();
        }

        private void AddCombatModeIndicatorsOverlay()
        {
            _overlayManager.AddOverlay(new ShowCombatModeIndicatorsOverlay(
                _inputManager,
                EntityManager,
                _eye,
                this));
        }
    }
}
