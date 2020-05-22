using Content.Client.GameObjects.Components.HUD.Hotbar;
using Content.Client.GameObjects.EntitySystems;
using Robust.Client.GameObjects.EntitySystems;
using Robust.Client.Player;
using Robust.Shared.Input;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.UserInterface
{
    public class HotbarManager : IHotbarManager
    {
#pragma warning disable 649
        [Dependency] private readonly IPlayerManager _playerManager;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager;
#pragma warning restore 649

        private InputCmdHandler _previousHandler;
        private Ability _boundAbility;

        private HotbarGui _hotbarGui;

        public HotbarManager()
        {
        }

        public void BindUse(Ability ability)
        {
            if (!_entitySystemManager.TryGetEntitySystem<InputSystem>(out var inputSys))
            {
                return;
            }

            if (inputSys.BindMap.TryGetHandler(EngineKeyFunctions.Use, out var handler))
            {
                _previousHandler = handler;
                inputSys.BindMap.UnbindFunction(EngineKeyFunctions.Use);
            }

            _boundAbility = ability;
            inputSys.BindMap.BindFunction(EngineKeyFunctions.Use,
                new PointerInputCmdHandler((in PointerInputCmdHandler.PointerInputCmdArgs args) => {
                    ability.Activate(args);
                    return true;
                }));
        }

        public void UnbindUse(Ability ability)
        {
            if (ability != _boundAbility)
            {
                return;
            }

            if (!_entitySystemManager.TryGetEntitySystem<InputSystem>(out var inputSys))
            {
                return;
            }

            _boundAbility = null;
            inputSys.BindMap.UnbindFunction(EngineKeyFunctions.Use);

            if (_previousHandler != null)
            {
                inputSys.BindMap.BindFunction(EngineKeyFunctions.Use, _previousHandler);
                _previousHandler = null;
            }

            if (_playerManager.LocalPlayer.ControlledEntity == null
                || !_playerManager.LocalPlayer.ControlledEntity.TryGetComponent(out HotbarComponent hotbarComponent))
            {
                return;
            }

            hotbarComponent.UnpressHotbarAbility(ability);
        }
    }
}
