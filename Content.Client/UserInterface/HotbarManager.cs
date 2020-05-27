using System;
using System.Collections.Generic;
using Content.Client.GameObjects.Components.HUD.Hotbar;
using Content.Client.GameObjects.EntitySystems;
using Robust.Client.GameObjects.EntitySystems;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Players;

namespace Content.Client.UserInterface
{
    public class HotbarManager : IHotbarManager
    {
#pragma warning disable 649
        [Dependency] private readonly IPlayerManager _playerManager;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager;
#pragma warning restore 649

        private Dictionary<string, HotbarAction> _hotbarActions;
        private InputCmdHandler _previousHandler;
        private HotbarAction _boundHotbarAction;

        private HotbarGui _hotbarGui;

        public HotbarManager()
        {
        }

        public void AddHotbarAction(string name, string texturePath,
            Action<ICommonSession, GridCoordinates, EntityUid, HotbarAction> activateAction,
            Action<bool> selectAction, TimeSpan? cooldown)
        {
        }

        public void RemoveAction(HotbarAction action)
        {
            // Maybe this can be replaced by a variable here set by the HotbarComponent OnPlayerAttached and OnPlayerDetached?
            if (_playerManager.LocalPlayer.ControlledEntity == null
                || !_playerManager.LocalPlayer.ControlledEntity.TryGetComponent(out HotbarComponent hotbarComponent))
            {
                return;
            }

            hotbarComponent.RemoveAction(action);
        }

        public void BindUse(HotbarAction action)
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

            _boundHotbarAction = action;
            inputSys.BindMap.BindFunction(EngineKeyFunctions.Use,
                new PointerInputCmdHandler((in PointerInputCmdHandler.PointerInputCmdArgs args) => {
                    action.Activate(args);
                    return true;
                }));
        }

        public void UnbindUse(HotbarAction action)
        {
            if (action != _boundHotbarAction)
            {
                return;
            }

            if (!_entitySystemManager.TryGetEntitySystem<InputSystem>(out var inputSys))
            {
                return;
            }

            _boundHotbarAction = null;
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

            hotbarComponent.UnpressHotbarAction(action);
        }
    }
}
