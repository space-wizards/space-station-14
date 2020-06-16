using System;
using System.Collections.Generic;
using Content.Client.GameObjects.Components.HUD.Hotbar;
using Content.Client.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.HUD.Hotbar;
using Robust.Client.GameObjects.EntitySystems;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
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
        [Dependency] private readonly IPrototypeManager _prototypeManager;
#pragma warning restore 649

        private Dictionary<HotbarActionId, HotbarAction> _hotbarActions;

        public void Initialize()
        {
            _hotbarActions = new Dictionary<HotbarActionId, HotbarAction>();
            foreach (var prototype in _prototypeManager.EnumeratePrototypes<HotbarActionPrototype>())
            {
                var hotbarAction = new HotbarAction(prototype.Name, prototype.TexturePath, ToggleAction, SelectAction);
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
            Action<bool> selectAction)
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
            CommandBinds.Builder
                .Bind(EngineKeyFunctions.Use,
                new PointerInputCmdHandler((in PointerInputCmdHandler.PointerInputCmdArgs args) => {
                    action.Activate(args);
                    return true;
                }))
                .Register<HotbarManager>();
        }

        public void UnbindUse(HotbarAction action)
        {
            CommandBinds.Unregister<HotbarManager>();

            if (_playerManager.LocalPlayer.ControlledEntity == null
                || !_playerManager.LocalPlayer.ControlledEntity.TryGetComponent(out HotbarComponent hotbarComponent))
            {
                return;
            }

            hotbarComponent.SetHotbarSlotPressed(hotbarComponent.GetSlotOf(action), false);
        }
    }
}
