using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Client.Administration.UI.ShutdownConfirm;

public sealed class ShutdownConfirmationWindow : DefaultWindow
{
    public readonly Button ConfirmButton;
    public readonly Button CancelButton;

    public ShutdownConfirmationWindow()
    {
        Title = "Confirm Shutdown";

        Contents.AddChild(new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Children =
                {
                    new Label
                    {
                        Text = "Are you sure you want to shut down the server?",
                        HorizontalAlignment = HAlignment.Center
                    },
                    new BoxContainer
                    {
                        Orientation = BoxContainer.LayoutOrientation.Horizontal,
                        HorizontalExpand = true,
                        Children =
                        {
                            (ConfirmButton = new Button
                            {
                                Text = "Confirm",
                                HorizontalAlignment = HAlignment.Center
                            }),

                            new Control { MinSize = new (10, 0) },

                            (CancelButton = new Button
                            {
                                Text = "Cancel",
                                HorizontalAlignment = HAlignment.Center
                            })
                        }
                    }
                }
        });
    }
}
