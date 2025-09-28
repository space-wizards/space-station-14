using System.Numerics;
using Content.Client.UserInterface.Controls;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client._Offbrand.MMI;

public sealed class MMIExtractorMenu : FancyWindow
{
    public readonly Button DenyButton;
    public readonly Button AcceptButton;

    public MMIExtractorMenu()
    {
        Title = Loc.GetString("mmi-extractor-title");

        ContentsContainer.AddChild(new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            Margin = new Thickness(6),
            Children =
            {
                (new Label()
                {
                    Text = Loc.GetString("mmi-extractor-prompt"),
                }),
                new BoxContainer
                {
                    Orientation = LayoutOrientation.Horizontal,
                    Align = AlignMode.Center,
                    Children =
                    {
                        (AcceptButton = new Button
                        {
                            Text = Loc.GetString("mmi-extractor-accept"),
                        }),

                        (new Control()
                        {
                            MinSize = new Vector2(20, 0)
                        }),

                        (DenyButton = new Button
                        {
                            Text = Loc.GetString("mmi-extractor-decline"),
                        })
                    }
                },
            }
        });
    }
}

