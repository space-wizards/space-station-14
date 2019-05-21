using Content.Client.GameObjects.Components.Mobs;
using Content.Shared.Input;
using Robust.Client.Interfaces.Input;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Utility;
using System.Collections.Generic;
using System.Linq;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.GameObjects.Components.Actor
{
    /// <summary>
    /// A semi-abstract component which gets added to entities upon attachment and collects all character
    /// user interfaces into a single window and keybind for the user
    /// </summary>
    public class CharacterInterface : Component
    {
        public override string Name => "Character Interface Component";

        /// <summary>
        /// Stored keybind to open the menu on keypress
        /// </summary>
        private InputCmdHandler _openMenuCmdHandler;

        /// <summary>
        /// Window to hold each of the character interfaces
        /// </summary>
        private SS14Window _window;

        /// <summary>
        /// Create the window with all character UIs and bind it to a keypress
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();

            //Use all the character ui interfaced components to create the character window
            var UIcomponents = Owner.GetAllComponents<ICharacterUI>();
            _window = new CharacterWindow(UIcomponents);

            _window.AddToScreen();

            //Toggle window visible/invisible on keypress
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

            //Set keybind to open character menu
            var inputMgr = IoCManager.Resolve<IInputManager>();
            inputMgr.SetInputCommand(ContentKeyFunctions.OpenCharacterMenu, _openMenuCmdHandler);
        }

        /// <summary>
        /// Dispose of window and the keypress binding
        /// </summary>
        public override void OnRemove()
        {
            base.OnRemove();

            _window.Dispose();
            _window = null;

            var inputMgr = IoCManager.Resolve<IInputManager>();
            inputMgr.SetInputCommand(ContentKeyFunctions.OpenCharacterMenu, null);
        }

        /// <summary>
        /// A window that collects and shows all the individual character user interfaces
        /// </summary>
        public class CharacterWindow : SS14Window
        {
            private readonly VBoxContainer _contentsVBox;

            public CharacterWindow(IEnumerable<ICharacterUI> windowComponents) : base(IoCManager.Resolve<IDisplayManager>())
            {
                Title = "Character";
                HideOnClose = true;
                Visible = false;

                _contentsVBox = new VBoxContainer();
                Contents.AddChild(_contentsVBox);

                // TODO: sort window components by priority of window component
                foreach (var element in windowComponents.OrderBy(x => x.Priority))
                {
                    _contentsVBox.AddChild(element.Scene);
                }

                Size = CombinedMinimumSize;
            }
        }
    }

    /// <summary>
    /// Determines ordering of the character user interface, small values come sooner
    /// </summary>
    public enum UIPriority
    {
        First = 0,
        Species = 100,
        Inventory = 200,
        Last = 99999
    }
}
