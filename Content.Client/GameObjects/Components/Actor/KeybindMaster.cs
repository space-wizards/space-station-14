using Content.Client.GameObjects.Components.Mobs;
using Content.Shared.Input;
using SS14.Client.Interfaces.Input;
using SS14.Client.UserInterface.CustomControls;
using SS14.Shared.GameObjects;
using SS14.Shared.Input;
using SS14.Shared.IoC;
using System.Collections.Generic;

namespace Content.Client.GameObjects.Components.Actor
{
    public class KeybindMaster : Component
    {
        public override string Name => "Keymaster Component";

        private InputCmdHandler _openMenuCmdHandler;
        private SS14Window _window;

        public override void Initialize()
        {
            base.Initialize();

            var UIcomponents = Owner.GetAllComponents<ICharacterUI>();
            _window = new CharacterWindow(UIcomponents);

            _window.AddToScreen();
            _openMenuCmdHandler = InputCmdHandler.FromDelegate(session => {
                if (_window.Visible)
                {
                    _window.Close();
                }
                else
                {
                    _window.Open();
                }
            });

            var inputMgr = IoCManager.Resolve<IInputManager>();
            inputMgr.SetInputCommand(ContentKeyFunctions.OpenCharacterMenu, _openMenuCmdHandler);
        }

        public override void OnRemove()
        {
            base.OnRemove();

            _window.Dispose();
            _window = null;

            var inputMgr = IoCManager.Resolve<IInputManager>();
            inputMgr.SetInputCommand(ContentKeyFunctions.OpenCharacterMenu, null);
        }

        public class CharacterWindow : SS14Window
        {
            public CharacterWindow(IEnumerable<ICharacterUI> windowcomponents)
            {
                //TODO: sort window components by priority of window component
                foreach(var element in windowcomponents)
                {
                    AddChild(element.Scene);
                }

                HideOnClose = true;
            }
        }
    }
}
