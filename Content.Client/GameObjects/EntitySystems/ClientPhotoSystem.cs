using Content.Client.GameObjects.Components.Photography;
using Content.Client.Interfaces.GameObjects;
using Robust.Client.GameObjects.EntitySystems;
using Robust.Client.Interfaces.Input;
using Robust.Client.Player;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Input;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;

namespace Content.Client.GameObjects.EntitySystems
{
    public class ClientPhotoSystem : EntitySystem
    {

#pragma warning disable 649
        [Dependency] private readonly IPlayerManager _playerManager = default;
        [Dependency] private readonly IInputManager _inputManager = default;
        [Dependency] private readonly IGameTiming _gameTiming = default;
#pragma warning restore 649

        private InputSystem _inputSystem;
        private bool _blocked;
        private bool _down;

        public override void Initialize()
        {
            base.Initialize();

            IoCManager.InjectDependencies(this);
            _inputSystem = EntitySystemManager.GetEntitySystem<InputSystem>();
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            if (!_gameTiming.IsFirstTimePredicted)
            {
                return;
            }

            var state = _inputSystem.CmdStates.GetState(EngineKeyFunctions.Use);
            if (state != BoundKeyState.Down)
            {
                _blocked = false;
                _down = false;
                return;
            }

            //Rapid fire photos - no thanks
            if (_down)
            {
                return;
            }
            _down = true;

            var entity = _playerManager.LocalPlayer.ControlledEntity;
            if (entity == null || !entity.TryGetComponent(out IHandsComponent hands))
            {
                _blocked = true;
                return;
            }

            var held = hands.ActiveHand;
            if (held == null || !held.TryGetComponent(out PhotoCameraComponent camera))
            {
                _blocked = true;
                return;
            }

            if (_blocked)
            {
                return;
            }

            camera.TryTakePhoto(entity.Uid, _inputManager.MouseScreenPosition);
        }
    }
}
