using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Localization;

namespace Content.Client.Administration
{
    public sealed class TicketWindow : SS14Window
    {
        public readonly Button CloseButton;
        public readonly Button ResolveButton;

        public TicketWindow()
        {

            Title = Loc.GetString("Ticket");

            Contents.AddChild(new VBoxContainer
            {
                Children =
                {
                    new VBoxContainer
                    {
                        Children =
                        {
                            (new Label()
                            {
                                Text = Loc.GetString("This UI is very WIP")
                            }),
                            new HBoxContainer
                            {
                                Align  = BoxContainer.AlignMode.Center,
                                Children =
                                {
                                    (CloseButton = new Button
                                    {
                                        Text = Loc.GetString("Close"),
                                    }),

                                    (new Control()
                                    {
                                        MinSize = (20, 0)
                                    }),

                                    (ResolveButton = new Button
                                    {
                                        Text = Loc.GetString("Resolve"),
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
