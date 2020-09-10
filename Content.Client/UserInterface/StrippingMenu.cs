using System;
using Content.Client.UserInterface.Stylesheets;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Client.UserInterface
{
    public class StrippingMenu : SS14Window
    {
        protected override Vector2? CustomSize => new Vector2(400, 600);

        private readonly VBoxContainer _vboxContainer;

        // title's and sizes a vbox container.
        public StrippingMenu(string title)
        {
            Title = title;

            _vboxContainer = new VBoxContainer()
            {
                SizeFlagsVertical = SizeFlags.FillExpand,
                SeparationOverride = 5,
            };

            Contents.AddChild(_vboxContainer);
        }

        // deletes all buttons so update can remake them.
        public void ClearButtons()
        {
            _vboxContainer.DisposeAllChildren();
        }

        // creates buttons for vbox.
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
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                SeparationOverride = 5,
                Children =
                {
                    new Label()
                    {
                        Text = $"{title}:"
                    },
                    new Control()
                    {
                        SizeFlagsHorizontal = SizeFlags.FillExpand
                    },
                    button,
                }
            });
        }
    }
}

// all in all i'm not sure this file needs to exist in the first place? a lot of the interactions are in SBUI.
// current plan is to steal from humaninvetoryinterfacecontroller's implementation of invetoryinterfacecontroller.
