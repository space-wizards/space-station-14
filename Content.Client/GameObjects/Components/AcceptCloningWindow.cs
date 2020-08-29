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

        public AcceptCloningWindow(ILocalizationManager loc)
        {
            var localization = loc;

            Title = localization.GetString("Cloning Machine");

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
                                Text = "You are being cloned! Transfer you soul to the clone body?"
                            }),
                            new HBoxContainer
                            {
                                Children =
                                {
                                    (ConfirmButton = new Button
                                    {
                                        Text = localization.GetString("Yes"),
                                    }),
                                    (DenyButton = new Button
                                    {
                                        Text = localization.GetString("No"),
                                    })
                                }
                            },
                        }
                    },
                }
            });
        }

        public override void Close()
        {
            base.Close();
            Dispose();
        }
    }
}
