using System.Linq;
using Content.Shared.GameObjects;
using Content.Shared.Input;
using SS14.Client.GameObjects.EntitySystems;
using SS14.Client.Interfaces.State;
using SS14.Client.State.States;
using SS14.Client.UserInterface.Controls;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Systems;
using SS14.Shared.Input;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Map;
using SS14.Shared.Maths;
using SS14.Shared.Players;
using SS14.Shared.Utility;

namespace Content.Client.GameObjects.EntitySystems
{
    public class VerbSystem : EntitySystem
    {
        [Dependency]
#pragma warning disable 649
        private readonly IStateManager _stateManager;
#pragma warning restore 649

        private Popup _currentPopup;

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
            var box = UIBox2.FromDimensions(args.ScreenCoordinates.Position - size / 2, size);
            _currentPopup.Open(box);
        }

        private void OnContextButtonPressed(IEntity entity)
        {
            DebugTools.AssertNotNull(_currentPopup);

            var vBox = _currentPopup.GetChild<VBoxContainer>("ButtonBox");
            vBox.DisposeAllChildren();

            foreach (var verb in entity.GetAllComponents<IVerbProvider>().SelectMany(p => p.GetVerbs(null)))
            {
                var button = new Button {Text = verb.Text, Disabled = !verb.Available};
                if (verb.Available)
                {
                    button.OnPressed += _ =>
                    {
                        _closeContextMenu();
                        verb.Callback();
                    };
                }

                vBox.AddChild(button);
            }
        }

        private void _closeContextMenu()
        {
            _currentPopup?.Dispose();
            _currentPopup = null;
        }
    }
}
