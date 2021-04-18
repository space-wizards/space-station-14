using System;
using System.Security.Claims;
using Content.Shared.Administration.Tickets;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using Robust.Shared.Utility;

namespace Content.Client.Administration
{
    public sealed class TicketWindow : SS14Window
    {
        public readonly Button ClaimTicketButton;
        public readonly Button CloseTicketButton;
        public readonly Button ResolveTicketButton;

        public Ticket? Ticket;
        public IPlayerSession? Session;
        public bool IsAdmin = false;
        public TicketStatus WindowStatus = TicketStatus.Unclaimed;

        public VBoxContainer Messages;
        public readonly LineEdit MessageInput;
        public readonly Button MessageSend;

        public void AddMessage(TicketMessage message)
        {
            var hBox = new HBoxContainer
            {
                HorizontalExpand = true,
                SeparationOverride = 4,
            };
            var textLabel = new RichTextLabel();
            var myTime = new DateTimeOffset(message.time, new TimeSpan(message.offset));
            var name = message.admin ? Ticket?.GetAdminName() : Ticket?.GetPlayerName();
            var text =
                $"[{myTime.ToLocalTime().ToString("HH:mm:ss")}] {name ?? "Unknown"}: {message.message}";
            if (message.admin)
            {
                textLabel.SetMessage(FormattedMessage.FromMarkup("[color=#ff0000]" + FormattedMessage.EscapeText(text) + "[/color]"));
            }
            else
            {
                textLabel.SetMessage(text);
            }
            hBox.AddChild(textLabel);

            Messages.AddChild(hBox);
            Messages.AddChild(new HSeparator());
        }


        public void LoadTicket(Ticket _ticket)
        {
            Ticket = _ticket;
            Title = Loc.GetString($"Ticket #{Ticket.Id.ToString()} - {Ticket.GetPlayerName()}");
            RefreshButtons();

            foreach (var message in Ticket.Messages)
            {
                AddMessage(message);
            }
        }

        public void RefreshButtons()
        {
            if (Ticket is null || Session is null)
            {
                return;
            }
            MessageSend.Disabled = !CanMessage();
            ClaimTicketButton.Text = Loc.GetString("Claim");
            if (WindowStatus == TicketStatus.Claimed)
            {
                if (Ticket.ClaimedAdmin == Session.UserId)
                {
                    ClaimTicketButton.Disabled = false;
                    ClaimTicketButton.Text = Loc.GetString("Unclaim");
                    CloseTicketButton.Disabled = false;
                    ResolveTicketButton.Disabled = false;
                }
                else
                {
                    ClaimTicketButton.Disabled = true;
                }
            }
            else
            {
                if (WindowStatus == TicketStatus.Unclaimed)
                {
                    ClaimTicketButton.Disabled = !IsAdmin || Session.UserId == Ticket.TargetPlayer;
                }
                else
                {
                    ClaimTicketButton.Disabled = true;
                }

                CloseTicketButton.Disabled = true;
                ResolveTicketButton.Disabled = true;
            }
        }

        public bool CanMessage()
        {
            if (Ticket is null || Session is null || WindowStatus == TicketStatus.Closed || WindowStatus == TicketStatus.Resolved)
            {
                return false;
            }

            if (Session.UserId != Ticket.ClaimedAdmin && Session.UserId != Ticket.TargetPlayer)
            {
                return false;
            }

            return true;
        }

        public TicketWindow()
        {
            Title = Loc.GetString("Ticket");
            MinSize = (400, 400);

            var mainBox = new VBoxContainer
            {
                Children =
                {
                    new HBoxContainer
                    {
                        VerticalAlignment = VAlignment.Top,
                        HorizontalAlignment = HAlignment.Center,
                        Children =
                        {
                            (ClaimTicketButton = new Button
                            {
                                Text = Loc.GetString("Claim"),
                            }),

                            (new Control()
                            {
                                MinSize = (20, 0)
                            }),
                            (CloseTicketButton = new Button
                            {
                                Text = Loc.GetString("Close"),
                            }),

                            (new Control()
                            {
                                MinSize = (20, 0)
                            }),

                            (ResolveTicketButton = new Button
                            {
                                Text = Loc.GetString("Resolve"),
                            })
                        }
                    },
                }
            };
            Contents.AddChild(mainBox);
            mainBox.AddChild(new Control {MinSize = (0, 10)}); //Padding
            var panel = new PanelContainer()
            {
                PanelOverride = new StyleBoxFlat {BackgroundColor = Color.FromHex("#202028")},
                VerticalExpand = true,
                //MaxHeight = 300,
                Children =
                {
                    new ScrollContainer()
                    {
                        SizeFlagsStretchRatio = 8,
                        //VerticalExpand = true,
                        //HorizontalExpand = true,
                        //MaxHeight = 300,
                        MinSize = (120, 200),
                        HScrollEnabled = false,
                        Children =
                        {
                            (Messages = new VBoxContainer())
                        }
                    }
                }
            };
            mainBox.AddChild(panel);
            mainBox.AddChild(new Control {MinSize = (0, 10)}); //Padding
            var chatInput = new HBoxContainer
            {
                Children =
                {
                    (MessageInput = new LineEdit
                    {
                        HorizontalExpand = true,
                        MinSize = (120, 0),
                        PlaceHolder = Loc.GetString("Create Message..."),
                    }),
                    new Control {MinSize = (10, 0)},
                    (MessageSend = new Button {Text = Loc.GetString("Send"), })
                }
            };
            mainBox.AddChild(chatInput);
            mainBox.AddChild(new Control {MinSize = (0, 5)}); //Padding
        }

        private class HSeparator : Control
        {
            public HSeparator()
            {
                AddChild(new PanelContainer
                {
                    PanelOverride = new StyleBoxFlat
                    {
                        BackgroundColor = Color.FromHex("#3D4059"),
                        ContentMarginBottomOverride = 2, ContentMarginLeftOverride = 2
                    },
                    MaxHeight = 2f,
                    VerticalAlignment = VAlignment.Top
                });
            }
        }
    }
}
