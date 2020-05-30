using System;
using System.Collections.Generic;
using Content.Client.GameObjects.Components.HUD.Hotbar;
using Content.Client.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.HUD.Hotbar;
using Robust.Client.GameObjects.EntitySystems;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;

namespace Content.Client.UserInterface
{
    public class HotbarManager : IHotbarManager
    {
#pragma warning disable 649
        [Dependency] private readonly IPlayerManager _playerManager;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager;
        [Dependency] private readonly IPrototypeManager _prototypeManager;
#pragma warning restore 649

        private Dictionary<HotbarActionId, HotbarAction> _hotbarActions;
        private InputCmdHandler _previousHandler;
        private HotbarAction _boundHotbarAction;

        public void Initialize()
        {
            _hotbarActions = new Dictionary<HotbarActionId, HotbarAction>();
            foreach (var prototype in _prototypeManager.EnumeratePrototypes<HotbarActionPrototype>())
            {
                var hotbarAction = new HotbarAction(prototype.Name, prototype.TexturePath, ToggleAction, SelectAction, new TimeSpan(0));
                hotbarAction.Id = prototype.HotbarActionId;
                _hotbarActions.Add(prototype.HotbarActionId, hotbarAction);
            }
        }

        public IReadOnlyDictionary<HotbarActionId, HotbarAction> GetGlobalActions()
        {
            return _hotbarActions;
        }

        private void SelectAction(HotbarAction action, bool enabled)
        {
            // Solve implementation and show enabled/disabled
            if (_playerManager.LocalPlayer.ControlledEntity == null
                || !_playerManager.LocalPlayer.ControlledEntity.TryGetComponent(out HotbarComponent hotbarComponent))
            {
                return;
            }

            _playerManager.LocalPlayer.ControlledEntity.SendNetworkMessage(hotbarComponent, new HotbarActionMessage(action.Id, enabled));
        }

        private void ToggleAction(HotbarAction action, ICommonSession session, GridCoordinates coords, EntityUid uid)
        {
            if (_playerManager.LocalPlayer.ControlledEntity == null
                || !_playerManager.LocalPlayer.ControlledEntity.TryGetComponent(out HotbarComponent hotbarComponent))
            {
                return;
            }
            hotbarComponent.SetHotbarSlotPressed(hotbarComponent.GetSlotOf(action), !action.Active);
            SelectAction(action, action.Active);
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

            hotbarComponent.RemoveActionFromMenu(action);
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

            hotbarComponent.SetHotbarSlotPressed(hotbarComponent.GetSlotOf(action), false);
        }
    }
}
