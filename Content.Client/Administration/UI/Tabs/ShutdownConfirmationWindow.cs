using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Administration.UI
{
    public sealed class ShutdownConfirmationWindow : DefaultWindow
    {
        public readonly Button ConfirmButton;
        public readonly Button CancelButton;

        public ShutdownConfirmationWindow()
        {
            Title = "Confirm Shutdown";

            Contents.AddChild(new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                Children =
                {
                    new Label { Text = "Назад дороги нет" },
                    new BoxContainer
                    {
                        Orientation = LayoutOrientation.Horizontal,
                        Children =
                        {
                            (ConfirmButton = new Button { Text = "Отключить сервер" }),
                            new Control { MinSize = new (20, 0) },
                            (CancelButton = new Button { Text = "Отмена" })
                        }
                    }
                }
            });
        }
    }
}
