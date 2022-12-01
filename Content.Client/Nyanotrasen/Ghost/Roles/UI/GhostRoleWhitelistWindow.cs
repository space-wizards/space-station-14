using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Ghost.Roles.UI
{
    public sealed class GhostRoleWhitelistWindow : DefaultWindow
    {
        public readonly Button DenyButton;
        public readonly Button AcceptButton;

        public GhostRoleWhitelistWindow()
        {

            Title = Loc.GetString("ghost-role-whitelist-title");

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
                                Text = (Loc.GetString("ghost-role-whitelist-text") + "\n")
                            }),
                            new BoxContainer
                            {
                                Orientation = LayoutOrientation.Horizontal,
                                Align = AlignMode.Center,
                                Children =
                                {
                                    (AcceptButton = new Button
                                    {
                                        Text = Loc.GetString("ui-escape-discord"),
                                    }),

                                    (new Control()
                                    {
                                        MinSize = (20, 0)
                                    }),

                                    (DenyButton = new Button
                                    {
                                        Text = Loc.GetString("ghost-role-whitelist-OK"),
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
