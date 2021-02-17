using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Stylesheets;
using Content.Client.Utility;
using Robust.Client;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using static Content.Client.StaticIoC;

namespace Content.Client.State
{
    public class LauncherConnecting : Robust.Client.State.State
    {
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private readonly IStylesheetManager _stylesheetManager = default!;
        [Dependency] private readonly IClientNetManager _clientNetManager = default!;
        [Dependency] private readonly IGameController _gameController = default!;
        [Dependency] private readonly IBaseClient _baseClient = default!;

        private Control _control;
        private Label _connectStatus;

        private Control _connectingStatus;
        private Control _connectFail;
        private Label _connectFailReason;
        private Control _disconnected;

        public override void Startup()
        {
            Button exitButton;
            Button reconnectButton;
            Button retryButton;

            var address = _gameController.LaunchState.Ss14Address ?? _gameController.LaunchState.ConnectAddress;

            _control = new Control
            {
                Stylesheet = _stylesheetManager.SheetSpace,
                Children =
                {
                    new PanelContainer {StyleClasses = {StyleBase.ClassAngleRect}},
                    new VBoxContainer
                    {
                        SeparationOverride = 0,
                        CustomMinimumSize = (300, 200),
                        Children =
                        {
                            new HBoxContainer
                            {
                                Children =
                                {
                                    new MarginContainer
                                    {
                                        MarginLeftOverride = 8,
                                        Children =
                                        {
                                            new Label
                                            {
                                                Text = Loc.GetString("Space Station 14"),
                                                StyleClasses = {StyleBase.StyleClassLabelHeading},
                                                VAlign = Label.VAlignMode.Center
                                            },
                                        }
                                    },

                                    (exitButton = new Button
                                    {
                                        Text = Loc.GetString("Exit"),
                                        SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd | Control.SizeFlags.Expand
                                    }),
                                }
                            },

                            // Line
                            new HighDivider(),

                            new MarginContainer
                            {
                                SizeFlagsVertical = Control.SizeFlags.FillExpand,
                                MarginLeftOverride = 4,
                                MarginRightOverride = 4,
                                MarginTopOverride = 4,
                                Children =
                                {
                                    new VBoxContainer
                                    {
                                        SeparationOverride = 0,
                                        Children =
                                        {
                                            new Control
                                            {
                                                SizeFlagsVertical = Control.SizeFlags.FillExpand,
                                                Children =
                                                {
                                                    (_connectingStatus = new VBoxContainer
                                                    {
                                                        SeparationOverride = 0,
                                                        Children =
                                                        {
                                                            new Label
                                                            {
                                                                Text = Loc.GetString("Connecting to server..."),
                                                                Align = Label.AlignMode.Center,
                                                            },

                                                            (_connectStatus = new Label
                                                            {
                                                                StyleClasses = {StyleBase.StyleClassLabelSubText},
                                                                Align = Label.AlignMode.Center,
                                                            }),
                                                        }
                                                    }),
                                                    (_connectFail = new VBoxContainer
                                                    {
                                                        Visible = false,
                                                        SeparationOverride = 0,
                                                        Children =
                                                        {
                                                            (_connectFailReason = new Label
                                                            {
                                                                Align = Label.AlignMode.Center
                                                            }),

                                                            (retryButton = new Button
                                                            {
                                                                Text = "Retry",
                                                                SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
                                                                SizeFlagsVertical =
                                                                    Control.SizeFlags.Expand |
                                                                    Control.SizeFlags.ShrinkEnd
                                                            })
                                                        }
                                                    }),

                                                    (_disconnected = new VBoxContainer
                                                    {
                                                        SeparationOverride = 0,
                                                        Children =
                                                        {
                                                            new Label
                                                            {
                                                                Text = "Disconnected from server:",
                                                                Align = Label.AlignMode.Center
                                                            },
                                                            new Label
                                                            {
                                                                Text = _baseClient.LastDisconnectReason,
                                                                Align = Label.AlignMode.Center
                                                            },
                                                            (reconnectButton = new Button
                                                            {
                                                                Text = "Reconnect",
                                                                SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
                                                                SizeFlagsVertical =
                                                                    Control.SizeFlags.Expand |
                                                                    Control.SizeFlags.ShrinkEnd
                                                            })
                                                        }
                                                    })
                                                }
                                            },

                                            // Padding.
                                            new Control {CustomMinimumSize = (0, 8)},

                                            new Label
                                            {
                                                Text = address,
                                                StyleClasses = {StyleBase.StyleClassLabelSubText},
                                                SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
                                            }
                                        }
                                    },
                                }
                            },

                            // Line
                            new PanelContainer
                            {
                                PanelOverride = new StyleBoxFlat
                                {
                                    BackgroundColor = Color.FromHex("#444"),
                                    ContentMarginTopOverride = 2
                                },
                            },

                            new MarginContainer
                            {
                                MarginLeftOverride = 12,
                                MarginRightOverride = 4,
                                Children =
                                {
                                    new HBoxContainer
                                    {
                                        SizeFlagsVertical = Control.SizeFlags.ShrinkEnd,
                                        Children =
                                        {
                                            new Label
                                            {
                                                Text = Loc.GetString("Don't die!"),
                                                StyleClasses = {StyleBase.StyleClassLabelSubText}
                                            },
                                            new Label
                                            {
                                                Text = "ver 0.1",
                                                SizeFlagsHorizontal =
                                                    Control.SizeFlags.Expand | Control.SizeFlags.ShrinkEnd,
                                                StyleClasses = {StyleBase.StyleClassLabelSubText}
                                            }
                                        }
                                    }
                                }
                            },
                        }
                    },
                }
            };

            _userInterfaceManager.StateRoot.AddChild(_control);

            LayoutContainer.SetAnchorPreset(_control, LayoutContainer.LayoutPreset.Center);
            LayoutContainer.SetGrowHorizontal(_control, LayoutContainer.GrowDirection.Both);
            LayoutContainer.SetGrowVertical(_control, LayoutContainer.GrowDirection.Both);

            exitButton.OnPressed += args =>
            {
                _gameController.Shutdown("Exit button pressed");
            };

            void Retry(BaseButton.ButtonEventArgs args)
            {
                _baseClient.ConnectToServer(_gameController.LaunchState.ConnectEndpoint);
                SetActivePage(Page.Connecting);
            }

            reconnectButton.OnPressed += Retry;
            retryButton.OnPressed += Retry;

            _clientNetManager.ConnectFailed += (sender, args) =>
            {
                _connectFailReason.Text = Loc.GetString("Failed to connect to server:\n{0}", args.Reason);
                SetActivePage(Page.ConnectFailed);
            };

            _clientNetManager.ClientConnectStateChanged += ConnectStateChanged;

            SetActivePage(Page.Connecting);

            ConnectStateChanged(_clientNetManager.ClientConnectState);
        }

        private void ConnectStateChanged(ClientConnectionState state)
        {
            _connectStatus.Text = Loc.GetString(state switch
            {
                ClientConnectionState.NotConnecting => "You should not be seeing this",
                ClientConnectionState.ResolvingHost => "Resolving server address...",
                ClientConnectionState.EstablishingConnection => "Establishing initial connection...",
                ClientConnectionState.Handshake => "Doing handshake...",
                ClientConnectionState.Connected => "Synchronizing game state...",
                _ => state.ToString()
            });
        }

        public override void Shutdown()
        {
            _control.Dispose();
        }

        public void SetDisconnected()
        {
            SetActivePage(Page.Disconnected);
        }

        private void SetActivePage(Page page)
        {
            _connectingStatus.Visible = page == Page.Connecting;
            _connectFail.Visible = page == Page.ConnectFailed;
            _disconnected.Visible = page == Page.Disconnected;
        }

        private enum Page : byte
        {
            Connecting,
            ConnectFailed,
            Disconnected,
        }
    }
}
