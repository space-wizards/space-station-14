using System;
using System.Collections.Generic;
using System.Reflection;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.EntitySystemMessages;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Client.GameObjects.EntitySystems;
using Robust.Client.Interfaces.Input;
using Robust.Client.Interfaces.State;
using Robust.Client.Interfaces.UserInterface;
using Robust.Client.Player;
using Robust.Client.State.States;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Input;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Client.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public sealed class VerbSystem : EntitySystem
    {
#pragma warning disable 649
        [Dependency] private readonly IStateManager _stateManager;
        [Dependency] private readonly IEntityManager _entityManager;
        [Dependency] private readonly IPlayerManager _playerManager;
        [Dependency] private readonly IInputManager _inputManager;
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager;
#pragma warning restore 649

        private VerbPopup _currentPopup;
        private EntityUid _currentEntity;

        public override void Initialize()
        {
            base.Initialize();

            IoCManager.InjectDependencies(this);

            var input = EntitySystemManager.GetEntitySystem<InputSystem>();
            input.BindMap.BindFunction(ContentKeyFunctions.OpenContextMenu,
                new PointerInputCmdHandler(OnOpenContextMenu));
        }

        public override void RegisterMessageTypes()
        {
            base.RegisterMessageTypes();

            RegisterMessageType<VerbSystemMessages.VerbsResponseMessage>();
        }

        public void OpenContextMenu(IEntity entity, ScreenCoordinates screenCoordinates)
        {
            if (_currentPopup != null)
            {
                _closeContextMenu();
            }

            _currentEntity = entity.Uid;
            _currentPopup = new VerbPopup();
            _currentPopup.UserInterfaceManager.StateRoot.AddChild(_currentPopup);
            _currentPopup.OnPopupHide += _closeContextMenu;

            _currentPopup.List.AddChild(new Label {Text = "Waiting on Server..."});
            RaiseNetworkEvent(new VerbSystemMessages.RequestVerbsMessage(_currentEntity));

            var size = _currentPopup.List.CombinedMinimumSize;
            var box = UIBox2.FromDimensions(screenCoordinates.Position, size);
            _currentPopup.Open(box);
        }

        private bool OnOpenContextMenu(in PointerInputCmdHandler.PointerInputCmdArgs args)
        {
            if (_currentPopup != null)
            {
                _closeContextMenu();
                return true;
            }

            if (!(_stateManager.CurrentState is GameScreen gameScreen))
            {
                return false;
            }

            var entities = gameScreen.GetEntitiesUnderPosition(args.Coordinates);

            if (entities.Count == 0)
            {
                return false;
            }

            _currentPopup = new VerbPopup();
            _currentPopup.OnPopupHide += _closeContextMenu;
            foreach (var entity in entities)
            {
                var button = new Button {Text = entity.Name};
                _currentPopup.List.AddChild(button);
                button.OnPressed += _ => OnContextButtonPressed(entity);
            }

            _currentPopup.UserInterfaceManager.StateRoot.AddChild(_currentPopup);

            var size = _currentPopup.List.CombinedMinimumSize;
            var box = UIBox2.FromDimensions(args.ScreenCoordinates.Position, size);
            _currentPopup.Open(box);

            return true;
        }

        private void OnContextButtonPressed(IEntity entity)
        {
            OpenContextMenu(entity, new ScreenCoordinates(_inputManager.MouseScreenPosition));
        }

        public override void HandleNetMessage(INetChannel channel, EntitySystemMessage message)
        {
            base.HandleNetMessage(channel, message);

            switch (message)
            {
                case VerbSystemMessages.VerbsResponseMessage resp:
                    _fillEntityPopup(resp);
                    break;
            }
        }

        private void _fillEntityPopup(VerbSystemMessages.VerbsResponseMessage msg)
        {
            if (_currentEntity != msg.Entity || !_entityManager.TryGetEntity(_currentEntity, out var entity))
            {
                return;
            }

            DebugTools.AssertNotNull(_currentPopup);

            var buttons = new List<Button>();

            var vBox = _currentPopup.List;
            vBox.DisposeAllChildren();
            foreach (var data in msg.Verbs)
            {
                var button = new Button {Text = data.Text, Disabled = !data.Available};
                if (data.Available)
                {
                    button.OnPressed += _ =>
                    {
                        RaiseNetworkEvent(new VerbSystemMessages.UseVerbMessage(_currentEntity, data.Key));
                        _closeContextMenu();
                    };
                }

                buttons.Add(button);
            }

            var user = GetUserEntity();
            //Get verbs, component dependent.
            foreach (var (component, verb) in VerbUtility.GetVerbs(entity))
            {
                if (verb.RequireInteractionRange && !VerbUtility.InVerbUseRange(user, entity))
                    continue;

                var disabled = verb.GetVisibility(user, component) != VerbVisibility.Visible;
                buttons.Add(CreateVerbButton(verb.GetText(user, component), disabled, verb.ToString(),
                    entity.ToString(), () => verb.Activate(user, component)));
            }
            //Get global verbs. Visible for all entities regardless of their components.
            foreach (var globalVerb in VerbUtility.GetGlobalVerbs(Assembly.GetExecutingAssembly()))
            {
                if (globalVerb.RequireInteractionRange && !VerbUtility.InVerbUseRange(user, entity))
                    continue;

                var disabled = globalVerb.GetVisibility(user, entity) != VerbVisibility.Visible;
                buttons.Add(CreateVerbButton(globalVerb.GetText(user, entity), disabled, globalVerb.ToString(),
                    entity.ToString(), () => globalVerb.Activate(user, entity)));
            }

            if (buttons.Count > 0)
            {
                buttons.Sort((a, b) => string.Compare(a.Text, b.Text, StringComparison.Ordinal));

                foreach (var button in buttons)
                {
                    vBox.AddChild(button);
                }
            }
            else
            {
                var panel = new PanelContainer();
                panel.AddChild(new Label {Text = "No verbs!"});
                vBox.AddChild(panel);
            }

            _currentPopup.Size = vBox.CombinedMinimumSize;

            // If we're at the bottom of the window and the menu would go below the bottom of the window,
            // shift it up so it extends UP.
            var bottomCoords = vBox.CombinedMinimumSize.Y + _currentPopup.Position.Y;
            if (bottomCoords > _userInterfaceManager.StateRoot.Size.Y)
            {
                _currentPopup.Position = _currentPopup.Position - new Vector2(0, vBox.CombinedMinimumSize.Y);
            }
        }

        private Button CreateVerbButton(string text, bool disabled, string verbName, string ownerName, Action action)
        {
            var button = new Button
            {
                Text = text,
                Disabled = disabled
            };
            if (!disabled)
            {
                button.OnPressed += _ =>
                {
                    _closeContextMenu();
                    try
                    {
                        action.Invoke();
                    }
                    catch (Exception e)
                    {
                        Logger.ErrorS("verb", "Exception in verb {0} on {1}:\n{2}", verbName, ownerName, e);
                    }
                };
            }
            return button;
        }

        private void _closeContextMenu()
        {
            _currentPopup?.Dispose();
            _currentPopup = null;
            _currentEntity = EntityUid.Invalid;
        }

        private IEntity GetUserEntity()
        {
            return _playerManager.LocalPlayer.ControlledEntity;
        }

        private sealed class VerbPopup : Popup
        {
            public VBoxContainer List { get; }

            public VerbPopup()
            {
                AddChild(List = new VBoxContainer());
            }
        }
    }
}
