using Content.Client.GameObjects.Components.Weapons.Ranged;
using Content.Client.Interfaces.GameObjects;
using Content.Shared.Input;
using SS14.Client.GameObjects.EntitySystems;
using SS14.Client.Interfaces.Graphics.ClientEye;
using SS14.Client.Interfaces.Input;
using SS14.Client.Player;
using SS14.Shared.GameObjects.Systems;
using SS14.Shared.Input;
using SS14.Shared.IoC;

namespace Content.Client.GameObjects.EntitySystems
{
    public class RangedWeaponSystem : EntitySystem
    {

#pragma warning disable 649
        [Dependency] private readonly IPlayerManager _playerManager;
        [Dependency] private readonly IEyeManager _eyeManager;
        [Dependency] private readonly IInputManager _inputManager;
#pragma warning restore 649

        private InputSystem _inputSystem;
        private bool _isFirstShot;
        private bool _blocked;

        public override void Initialize()
        {
            base.Initialize();

            IoCManager.InjectDependencies(this);
            _inputSystem = EntitySystemManager.GetEntitySystem<InputSystem>();
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var canFireSemi = _isFirstShot;
            var state = _inputSystem.CmdStates.GetState(ContentKeyFunctions.UseItemInHand);
            if (state != BoundKeyState.Down)
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
                weapon.TryFire(worldPos);
            }
        }
    }
}
