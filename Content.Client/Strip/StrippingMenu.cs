using System;
using Content.Client.Stylesheets;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Strip
{
    public sealed class StrippingMenu : DefaultWindow
    {
        private readonly BoxContainer _vboxContainer;

        public StrippingMenu(string title)
        {
            MinSize = SetSize = (400, 600);
            Title = title;

            _vboxContainer = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
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

            _vboxContainer.AddChild(new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
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
