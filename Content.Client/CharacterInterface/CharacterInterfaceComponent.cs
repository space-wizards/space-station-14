using System.Collections.Generic;
using Content.Client.CharacterInfo.Components;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.CharacterInterface
{
    /// <summary>
    /// A semi-abstract component which gets added to entities upon attachment and collects all character
    /// user interfaces into a single window and keybind for the user
    /// </summary>
    [RegisterComponent]
    public class CharacterInterfaceComponent : Component
    {
        public override string Name => "Character Interface Component";

        /// <summary>
        ///     Window to hold each of the character interfaces
        /// </summary>
        /// <remarks>
        ///     Null if it would otherwise be empty.
        /// </remarks>
        public CharacterWindow? Window { get; set; }

        public List<ICharacterUI>? UIComponents;

        /// <summary>
        /// A window that collects and shows all the individual character user interfaces
        /// </summary>
        public class CharacterWindow : DefaultWindow
        {
            private readonly List<ICharacterUI> _windowComponents;

            public CharacterWindow(List<ICharacterUI> windowComponents)
            {
                Title = "Character";

                var contentsVBox = new BoxContainer
                {
                    Orientation = LayoutOrientation.Vertical
                };

                var mainScrollContainer = new ScrollContainer { };
                mainScrollContainer.AddChild(contentsVBox);

                Contents.AddChild(mainScrollContainer);

                windowComponents.Sort((a, b) => ((int) a.Priority).CompareTo((int) b.Priority));
                foreach (var element in windowComponents)
                {
                    contentsVBox.AddChild(element.Scene);
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
