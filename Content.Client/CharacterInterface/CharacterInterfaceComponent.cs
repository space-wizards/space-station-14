using System.Collections.Generic;
using System.Linq;
using Content.Client.CharacterInfo.Components;
using Content.Client.HUD;
using Content.Shared.Input;
using Robust.Client.GameObjects;
using Robust.Client.Input;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.CharacterInterface
{
    /// <summary>
    /// A semi-abstract component which gets added to entities upon attachment and collects all character
    /// user interfaces into a single window and keybind for the user
    /// </summary>
    [RegisterComponent]
    public class CharacterInterfaceComponent : Component
    {
        [Dependency] private readonly IGameHud _gameHud = default!;

        public override string Name => "Character Interface Component";

        /// <summary>
        ///     Window to hold each of the character interfaces
        /// </summary>
        /// <remarks>
        ///     Null if it would otherwise be empty.
        /// </remarks>
        public CharacterWindow? Window { get; private set; }

        private List<ICharacterUI>? _uiComponents;

        /// <summary>
        /// Create the window with all character UIs and bind it to a keypress
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();

            //Use all the character ui interfaced components to create the character window
            _uiComponents = Owner.GetAllComponents<ICharacterUI>().ToList();
            if (_uiComponents.Count == 0)
            {
                return;
            }

            Window = new CharacterWindow(_uiComponents);
            Window.OnClose += () => _gameHud.CharacterButtonDown = false;
        }

        /// <summary>
        /// Dispose of window and the keypress binding
        /// </summary>
        public override void OnRemove()
        {
            base.OnRemove();

            if (_uiComponents != null)
            {
                foreach (var component in _uiComponents)
                {
                    // Make sure these don't get deleted when the window is disposed.
                    component.Scene.Orphan();
                }
            }

            _uiComponents = null;

            Window?.Close();
            Window = null;

            var inputMgr = IoCManager.Resolve<IInputManager>();
            inputMgr.SetInputCommand(ContentKeyFunctions.OpenCharacterMenu, null);
        }

        public void PlayerDetached()
        {
            if (Window != null)
            {
                _gameHud.CharacterButtonVisible = false;
                Window.Close();
            }
        }

        public void PlayerAttached()
        {
            if (Window != null)
            {
                _gameHud.CharacterButtonVisible = true;

                _gameHud.CharacterButtonToggled = b =>
                {
                    if (b)
                    {
                        Window.OpenCentered();
                    }
                    else
                    {
                        Window.Close();
                    }
                };
            }
        }

        /// <summary>
        /// A window that collects and shows all the individual character user interfaces
        /// </summary>
        public class CharacterWindow : SS14Window
        {
            private readonly VBoxContainer _contentsVBox;
            private readonly List<ICharacterUI> _windowComponents;

            public CharacterWindow(List<ICharacterUI> windowComponents)
            {
                Title = "Character";

                _contentsVBox = new VBoxContainer();
                Contents.AddChild(_contentsVBox);

                windowComponents.Sort((a, b) => ((int) a.Priority).CompareTo((int) b.Priority));
                foreach (var element in windowComponents)
                {
                    _contentsVBox.AddChild(element.Scene);
                }

                _windowComponents = windowComponents;
            }

            protected override void Opened()
            {
                base.Opened();
                foreach (var windowComponent in _windowComponents)
                {
                    windowComponent.Opened();
                }
            }
        }
    }

    /// <summary>
    /// Determines ordering of the character user interface, small values come sooner
    /// </summary>
    public enum UIPriority
    {
        First = 0,
        Info = 5,
        Species = 100,
        Last = 99999
    }
}
