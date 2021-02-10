using Content.Client.Chat;
using Content.Client.Interfaces;
using Content.Client.UserInterface.Stylesheets;
using Content.Client.Utility;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Maths;

namespace Content.Client.UserInterface
{
    internal sealed class LobbyGui : Control
    {
        public Label ServerName { get; }
        public Label StartTime { get; }
        public Button ReadyButton { get; }
        public Button ObserveButton { get; }
        public Button OptionsButton { get; }
        public Button LeaveButton { get; }
        public ChatBox Chat { get; }
        public LobbyPlayerList OnlinePlayerList { get; }
        public ServerInfo ServerInfo { get; }
        public LobbyCharacterPreviewPanel CharacterPreview { get; }

        public LobbyGui(IEntityManager entityManager,
            IResourceCache resourceCache,
            IClientPreferencesManager preferencesManager)
        {
            var margin = new MarginContainer
            {
                MarginBottomOverride = 20,
                MarginLeftOverride = 20,
                MarginRightOverride = 20,
                MarginTopOverride = 20,
            };

            AddChild(margin);

            var panelTex = resourceCache.GetTexture("/Textures/Interface/Nano/button.svg.96dpi.png");
            var back = new StyleBoxTexture
            {
                Texture = panelTex,
                Modulate = new Color(37, 37, 42),
            };
            back.SetPatchMargin(StyleBox.Margin.All, 10);

            var panel = new PanelContainer
            {
                PanelOverride = back
            };

            margin.AddChild(panel);

            var vBox = new VBoxContainer {SeparationOverride = 0};

            margin.AddChild(vBox);

            var topHBox = new HBoxContainer
            {
                CustomMinimumSize = (0, 40),
                Children =
                {
                    new MarginContainer
                    {
                        MarginLeftOverride = 8,
                        Children =
                        {
                            new Label
                            {
                                Text = Loc.GetString("Lobby"),
                                StyleClasses = {StyleNano.StyleClassLabelHeadingBigger},
                                VAlign = Label.VAlignMode.Center
                            }
                        }
                    },
                    (ServerName = new Label
                    {
                        StyleClasses = {StyleNano.StyleClassLabelHeadingBigger},
                        VAlign = Label.VAlignMode.Center,
                        SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.ShrinkCenter
                    }),
                    (OptionsButton = new Button
                    {
                        SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
                        Text = Loc.GetString("Options"),
                        StyleClasses = {StyleNano.StyleClassButtonBig},
                    }),
                    (LeaveButton = new Button
                    {
                        SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
                        Text = Loc.GetString("Leave"),
                        StyleClasses = {StyleNano.StyleClassButtonBig},
                    })
                }
            };

            vBox.AddChild(topHBox);

            vBox.AddChild(new PanelContainer
            {
                PanelOverride = new StyleBoxFlat
                {
                    BackgroundColor = StyleNano.NanoGold,
                    ContentMarginTopOverride = 2
                },
            });

            var hBox = new HBoxContainer
            {
                SizeFlagsVertical = SizeFlags.FillExpand,
                SeparationOverride = 0
            };
            vBox.AddChild(hBox);

            CharacterPreview = new LobbyCharacterPreviewPanel(
                entityManager,
                preferencesManager)
            {
                SizeFlagsHorizontal = SizeFlags.None
            };
            hBox.AddChild(new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                SeparationOverride = 0,
                Children =
                {
                    CharacterPreview,

                    new StripeBack
                    {
                        Children =
                        {
                            new MarginContainer
                            {
                                MarginRightOverride = 3,
                                MarginLeftOverride = 3,
                                MarginTopOverride = 3,
                                MarginBottomOverride = 3,
                                Children =
                                {
                                    new HBoxContainer
                                    {
                                        SeparationOverride = 6,
                                        Children =
                                        {
                                            (ObserveButton = new Button
                                            {
                                                Text = Loc.GetString("Observe"),
                                                StyleClasses = {StyleNano.StyleClassButtonBig}
                                            }),
                                            (StartTime = new Label
                                            {
                                                SizeFlagsHorizontal = SizeFlags.FillExpand,
                                                Align = Label.AlignMode.Right,
                                                FontColorOverride = Color.DarkGray,
                                                StyleClasses = {StyleNano.StyleClassLabelBig}
                                            }),
                                            (ReadyButton = new Button
                                            {
                                                ToggleMode = true,
                                                Text = Loc.GetString("Ready Up"),
                                                StyleClasses = {StyleNano.StyleClassButtonBig}
                                            }),
                                        }
                                    }
                                }
                            }
                        }
                    },

                    new MarginContainer
                    {
                        MarginRightOverride = 3,
                        MarginLeftOverride = 3,
                        MarginTopOverride = 3,
                        MarginBottomOverride = 3,
                        SizeFlagsVertical = SizeFlags.FillExpand,
                        Children =
                        {
                            (Chat = new ChatBox
                            {
                                Input = {PlaceHolder = Loc.GetString("Say something!")}
                            })
                        }
                    },
                }
            });

            hBox.AddChild(new PanelContainer
            {
                PanelOverride = new StyleBoxFlat {BackgroundColor = StyleNano.NanoGold}, CustomMinimumSize = (2, 0)
            });

            {
                hBox.AddChild(new VBoxContainer
                {
                    SizeFlagsHorizontal = SizeFlags.FillExpand,
                    Children =
                    {
                        new NanoHeading
                        {
                            Text = Loc.GetString("Online Players"),
                        },
                        new MarginContainer
                        {
                            SizeFlagsVertical = SizeFlags.FillExpand,
                            MarginRightOverride = 3,
                            MarginLeftOverride = 3,
                            MarginTopOverride = 3,
                            MarginBottomOverride = 3,
                            Children =
                            {
                                new HBoxContainer
                                {
                                    SizeFlagsHorizontal = SizeFlags.FillExpand,
                                    CustomMinimumSize = (50,50),
                                    Children =
                                    {
                                        (OnlinePlayerList = new LobbyPlayerList
                                        {
                                            SizeFlagsVertical = SizeFlags.FillExpand,
                                            SizeFlagsHorizontal = SizeFlags.FillExpand,
                                        })
                                    }
                                }
                            }
                        },
                        new NanoHeading
                        {
                            Text = Loc.GetString("Server Info"),
                        },
                        new MarginContainer
                        {
                            SizeFlagsVertical = SizeFlags.FillExpand,
                            MarginRightOverride = 3,
                            MarginLeftOverride = 3,
                            MarginTopOverride = 3,
                            MarginBottomOverride = 2,
                            Children =
                            {
                                (ServerInfo = new ServerInfo())
                            }
                        },
                    }
                });
            }
        }
    }

    public class LobbyPlayerList : Control
    {
        private readonly ScrollContainer _scroll;
        private readonly VBoxContainer _vBox;

        public LobbyPlayerList()
        {
            var panel = new PanelContainer()
            {
                PanelOverride = new StyleBoxFlat { BackgroundColor = Color.FromHex("#202028") },
            };
            _vBox = new VBoxContainer();
            _scroll = new ScrollContainer();
            _scroll.AddChild(_vBox);
            panel.AddChild(_scroll);
            AddChild(panel);
        }

        // Adds a row
        public void AddItem(string name, string status)
        {
            var hbox = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand,
            };

            // Player Name
            hbox.AddChild(new PanelContainer()
            {
                PanelOverride = new StyleBoxFlat
                {
                    BackgroundColor = Color.FromHex("#373744"),
                    ContentMarginBottomOverride = 2,
                    ContentMarginLeftOverride = 4,
                    ContentMarginRightOverride = 4,
                    ContentMarginTopOverride = 2
                },
                Children =
                {
                    new Label
                    {
                        Text = name
                    }
                },
                SizeFlagsHorizontal = SizeFlags.FillExpand
            });
            // Status
            hbox.AddChild(new PanelContainer()
            {
                PanelOverride = new StyleBoxFlat
                {
                    BackgroundColor = Color.FromHex("#373744"),
                    ContentMarginBottomOverride = 2,
                    ContentMarginLeftOverride = 4,
                    ContentMarginRightOverride = 4,
                    ContentMarginTopOverride = 2
                },
                Children =
                {
                    new Label
                    {
                        Text = status
                    }
                },
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                SizeFlagsStretchRatio = 0.2f,
            });

            _vBox.AddChild(hbox);
        }

        // Deletes all rows
        public void Clear()
        {
            _vBox.RemoveAllChildren();
        }
    }
}
