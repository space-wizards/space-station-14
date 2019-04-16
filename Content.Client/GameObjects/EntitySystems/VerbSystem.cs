using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.EntitySystemMessages;
using Content.Shared.Input;
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
    public class VerbSystem : EntitySystem
    {
#pragma warning disable 649
        [Dependency] private readonly IStateManager _stateManager;
        [Dependency] private readonly IEntityManager _entityManager;
        [Dependency] private readonly IPlayerManager _playerManager;
        [Dependency] private readonly IInputManager _inputManager;
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager;
#pragma warning restore 649

        private Popup _currentPopup;
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
            _currentPopup = new Popup();
            _currentPopup.UserInterfaceManager.StateRoot.AddChild(_currentPopup);
            _currentPopup.OnPopupHide += _closeContextMenu;
            var vBox = new VBoxContainer("ButtonBox");
            _currentPopup.AddChild(vBox);

            vBox.AddChild(new Label {Text = "Waiting on Server..."});
            RaiseNetworkEvent(new VerbSystemMessages.RequestVerbsMessage(_currentEntity));

            var size = vBox.CombinedMinimumSize;
            var box = UIBox2.FromDimensions(screenCoordinates.Position, size);
            _currentPopup.Open(box);
        }

        private void OnOpenContextMenu(in PointerInputCmdHandler.PointerInputCmdArgs args)
        {
            if (_currentPopup != null)
            {
                _closeContextMenu();
                return;
            }

            if (!(_stateManager.CurrentState is GameScreen gameScreen))
            {
                return;
            }

            var entities = gameScreen.GetEntitiesUnderPosition(args.Coordinates);

            _currentPopup = new Popup();
            _currentPopup.OnPopupHide += _closeContextMenu;
            var vBox = new VBoxContainer("ButtonBox");
            _currentPopup.AddChild(vBox);
            foreach (var entity in entities)
            {
                var button = new Button {Text = entity.Name};
                vBox.AddChild(button);
                button.OnPressed += _ => OnContextButtonPressed(entity);
            }

            _currentPopup.UserInterfaceManager.StateRoot.AddChild(_currentPopup);

            var size = vBox.CombinedMinimumSize;
            var box = UIBox2.FromDimensions(args.ScreenCoordinates.Position, size);
            _currentPopup.Open(box);
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

            var vBox = _currentPopup.GetChild<VBoxContainer>("ButtonBox");
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
            foreach (var (component, verb) in VerbUtility.GetVerbs(entity))
            {
                if (verb.RequireInteractionRange)
                {
                    var distanceSquared = (user.Transform.WorldPosition - entity.Transform.WorldPosition)
                        .LengthSquared;
                    if (distanceSquared > Verb.InteractionRangeSquared)
                    {
                        continue;
                    }
                }

                var disabled = verb.IsDisabled(user, component);
                var button = new Button
                {
                    Text = verb.GetText(user, component),
                    Disabled = disabled
                };
                if (!disabled)
                {
                    button.OnPressed += _ =>
                    {
                        _closeContextMenu();
                        try
                        {
                            verb.Activate(user, component);
                        }
                        catch (Exception e)
                        {
                            Logger.ErrorS("verb", "Exception in verb {0} on {1}:\n{2}", verb, entity, e);
                        }
                    };
                }

                buttons.Add(button);
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
            var bottomCoord = vBox.CombinedMinimumSize.Y + _currentPopup.Position.Y;
            if (bottomCoord > _userInterfaceManager.StateRoot.Size.Y)
            {
                _currentPopup.Position = _currentPopup.Position - new Vector2(0, vBox.CombinedMinimumSize.Y);
            }
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
    }
}
