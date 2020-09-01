#nullable enable
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Localization;

namespace Content.Client.GameObjects.Components
{
    public sealed class AcceptCloningWindow : SS14Window
    {
        public readonly Button DenyButton;
        public readonly Button ConfirmButton;

        public AcceptCloningWindow()
        {

            Title = Loc.GetString("Cloning Machine");

            Contents.AddChild(new VBoxContainer
            {
                Children =
                {
                    new VBoxContainer
                    {
                        Children =
                        {
                            (new Label
                            {
                                Text = Loc.GetString("You are being cloned! Transfer your soul to the clone body?")
                            }),
                            new HBoxContainer
                            {
                                Children =
                                {
                                    (ConfirmButton = new Button
                                    {
                                        Text = Loc.GetString("Yes"),
                                    }),
                                    (DenyButton = new Button
                                    {
                                        Text = Loc.GetString("No"),
                                    })
                                }
                            },
                        }
                    },
                }
            });
        }
    }
}
