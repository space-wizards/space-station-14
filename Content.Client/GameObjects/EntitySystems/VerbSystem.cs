using System;
using System.Linq;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.EntitySystemMessages;
using Content.Shared.Input;
using SS14.Client.GameObjects.EntitySystems;
using SS14.Client.Interfaces.State;
using SS14.Client.Player;
using SS14.Client.State.States;
using SS14.Client.UserInterface.Controls;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Systems;
using SS14.Shared.Input;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Map;
using SS14.Shared.Maths;
using SS14.Shared.Players;
using SS14.Shared.Utility;

namespace Content.Client.GameObjects.EntitySystems
{
    public class VerbSystem : EntitySystem
    {
#pragma warning disable 649
        [Dependency] private readonly IStateManager _stateManager;
        [Dependency] private readonly IEntityManager _entityManager;
        [Dependency] private readonly IPlayerManager _playerManager;
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
            DebugTools.AssertNotNull(_currentPopup);
            _currentEntity = entity.Uid;

            var vBox = _currentPopup.GetChild<VBoxContainer>("ButtonBox");
            vBox.DisposeAllChildren();

            RaiseNetworkEvent(new VerbSystemMessages.RequestVerbsMessage(_currentEntity));

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

                vBox.AddChild(button);
            }
        }

        public override void HandleNetMessage(INetChannel channel, EntitySystemMessage message)
        {
            base.HandleNetMessage(channel, message);

            switch (message)
            {
                case VerbSystemMessages.VerbsResponseMessage resp:
                    if (_currentEntity != resp.Entity)
                    {
                        return;
                    }

                    DebugTools.AssertNotNull(_currentPopup);

                    var vBox = _currentPopup.GetChild<VBoxContainer>("ButtonBox");
                    foreach (var data in resp.Verbs)
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

                        vBox.AddChild(button);
                    }

                    break;
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
