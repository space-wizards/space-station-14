using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Revolutionary.UI;

public sealed class DeconvertedMenu : DefaultWindow
{
    public readonly Button ConfirmButton;

    public DeconvertedMenu()
    {
        Title = Loc.GetString("rev-deconverted-title");

        Contents.AddChild(new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            Children =
            {
                new BoxContainer
                {
                    Orientation = LayoutOrientation.Vertical,
                    Children =
                    {
                        (new Label()
                        {
                            Text = Loc.GetString("rev-deconverted-text")
                        }),
                        new BoxContainer
                        {
                            Orientation = LayoutOrientation.Horizontal,
                            Align = AlignMode.Center,
                            Children =
                            {
                                (ConfirmButton = new Button
                                {
                                    Text = Loc.GetString("rev-deconverted-confirm")
                                })
                            }
                        },
                    }
                },
            }
        });
    }
}
