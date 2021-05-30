using System;
using Content.Client.UserInterface.Stylesheets;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Maths;

namespace Content.Client.UserInterface
{
    public class StrippingMenu : SS14Window
    {
        private readonly VBoxContainer _vboxContainer;

        public StrippingMenu(string title)
        {
            MinSize = SetSize = (400, 600);
            Title = title;

            _vboxContainer = new VBoxContainer()
            {
                VerticalExpand = true,
                SeparationOverride = 5,
            };

            Contents.AddChild(_vboxContainer);
        }

        public void ClearButtons()
        {
            _vboxContainer.DisposeAllChildren();
        }

        public void AddButton(string title, string name, Action<BaseButton.ButtonEventArgs> onPressed)
        {
            var button = new Button()
            {
                Text = name,
                StyleClasses = { StyleBase.ButtonOpenRight }
            };

            button.OnPressed += onPressed;

            _vboxContainer.AddChild(new HBoxContainer()
            {
                HorizontalExpand = true,
                SeparationOverride = 5,
                Children =
                {
                    new Label()
                    {
                        Text = $"{title}:"
                    },
                    new Control()
                    {
                        HorizontalExpand = true
                    },
                    button,
                }
            });
        }
    }
}
