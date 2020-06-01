using Content.Client.GameObjects.Components.Weapons.Ranged;
using Content.Client.Interfaces.GameObjects;
using Robust.Client.GameObjects.EntitySystems;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Client.Interfaces.Input;
using Robust.Client.Player;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Input;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Log;

namespace Content.Client.GameObjects.EntitySystems
{
    public class RangedWeaponSystem : EntitySystem
    {

#pragma warning disable 649
        [Dependency] private readonly IPlayerManager _playerManager;
        [Dependency] private readonly IEyeManager _eyeManager;
        [Dependency] private readonly IInputManager _inputManager;
        [Dependency] private readonly IGameTiming _gameTiming;
#pragma warning restore 649

        private InputSystem _inputSystem;
        private CombatModeSystem _combatModeSystem;
        private bool _isFirstShot;
        private bool _blocked;

        public override void Initialize()
        {
            base.Initialize();

            IoCManager.InjectDependencies(this);
            _inputSystem = EntitySystemManager.GetEntitySystem<InputSystem>();
            _combatModeSystem = EntitySystemManager.GetEntitySystem<CombatModeSystem>();
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            if (!_gameTiming.IsFirstTimePredicted)
            {
                return;
            }

            var canFireSemi = _isFirstShot;
            var state = _inputSystem.CmdStates.GetState(EngineKeyFunctions.Use);
            if (!_combatModeSystem.IsInCombatMode() || state != BoundKeyState.Down)
            {
                _isFirstShot = true;
                _blocked = false;
                return;
            }

            _isFirstShot = false;

            var entity = _playerManager.LocalPlayer.ControlledEntity;
            if (entity == null || !entity.TryGetComponent(out IHandsComponent hands))
            {
                return;
            }

            var held = hands.ActiveHand;
            if (held == null || !held.TryGetComponent(out ClientRangedWeaponComponent weapon))
            {
                _blocked = true;
                return;
            }

            if (_blocked)
            {
                return;
            }

            var worldPos = _eyeManager.ScreenToWorld(_inputManager.MouseScreenPosition);

            if (weapon.Automatic || canFireSemi)
            {
                Logger.Debug(IoCManager.Resolve<IGameTiming>().CurTick.ToString());
                weapon.SyncFirePos(worldPos);
            }
        }
    }
}
