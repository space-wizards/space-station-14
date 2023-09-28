using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Localization;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.SS220.DeathReminder.UI
{
    public sealed class DeathReminderWindow : DefaultWindow
    {
        public readonly Button AcceptButton;

        public DeathReminderWindow()
        {

            Title = Loc.GetString("death-reminder-window-title");

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
                                Text = Loc.GetString("death-reminder-window-prompt-text-part")
                            }),
                            new BoxContainer
                            {
                                Orientation = LayoutOrientation.Horizontal,
                                Align = AlignMode.Center,
                                Children =
                                {
                                    (AcceptButton = new Button
                                    {
                                        Text = Loc.GetString("death-reminder-window-accept-button"),
                                    }),

                                    (new Control()
                                    {
                                        MinSize = new Vector2(20, 0)
                                    }),


                                }
                            },
                        }
                    },
                }
            });
        }
    }
}
