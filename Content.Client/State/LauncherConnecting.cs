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

        private Control? _control;
        private Label? _connectStatus;

        private Control? _connectingStatus;
        private Control? _connectFail;
        private Label? _connectFailReason;
        private Control? _disconnected;

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
                        MinSize = (300, 200),
                        Children =
                        {
                            new HBoxContainer
                            {
                                Children =
                                {
                                    new Label
                                    {
                                        Margin = new Thickness(8, 0, 0, 0),
                                        Text = Loc.GetString("Space Station 14"),
                                        StyleClasses = {StyleBase.StyleClassLabelHeading},
                                        VAlign = Label.VAlignMode.Center
                                    },

                                    (exitButton = new Button
                                    {
                                        Text = Loc.GetString("Exit"),
                                        HorizontalAlignment = Control.HAlignment.Right,
                                        HorizontalExpand = true,
                                    }),
                                }
                            },

                            // Line
                            new HighDivider(),

                            new VBoxContainer
                            {
                                VerticalExpand = true,
                                Margin = new Thickness(4, 4, 4, 0),
                                SeparationOverride = 0,
                                Children =
                                {
                                    new Control
                                    {
                                        VerticalExpand = true,
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
                                                        HorizontalAlignment = Control.HAlignment.Center,
                                                        VerticalExpand = true,
                                                        VerticalAlignment = Control.VAlignment.Bottom,
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
                                                        HorizontalAlignment = Control.HAlignment.Center,
                                                        VerticalExpand = true,
                                                        VerticalAlignment = Control.VAlignment.Bottom,
                                                    })
                                                }
                                            })
                                        }
                                    },

                                    // Padding.
                                    new Control {MinSize = (0, 8)},

                                    new Label
                                    {
                                        Text = address,
                                        StyleClasses = {StyleBase.StyleClassLabelSubText},
                                        HorizontalAlignment = Control.HAlignment.Center
                                    }
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
                            new HBoxContainer
                            {
                                Margin = new Thickness(12, 0, 4, 0),
                                VerticalAlignment = Control.VAlignment.Bottom,
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
                                        HorizontalExpand = true,
                                        HorizontalAlignment = Control.HAlignment.Right,
                                        StyleClasses = {StyleBase.StyleClassLabelSubText}
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

            exitButton.OnPressed += _ =>
            {
                _gameController.Shutdown("Exit button pressed");
            };

            void Retry(BaseButton.ButtonEventArgs args)
            {
                if (_gameController.LaunchState.ConnectEndpoint != null)
                {
                    _baseClient.ConnectToServer(_gameController.LaunchState.ConnectEndpoint);
                    SetActivePage(Page.Connecting);
                }
            }

            reconnectButton.OnPressed += Retry;
            retryButton.OnPressed += Retry;

            _clientNetManager.ConnectFailed += (_, args) =>
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
            if (_connectStatus == null) return;

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
            _control?.Dispose();
        }

        public void SetDisconnected()
        {
            SetActivePage(Page.Disconnected);
        }

        private void SetActivePage(Page page)
        {
            if (_connectingStatus != null) _connectingStatus.Visible = page == Page.Connecting;
            if (_connectFail != null) _connectFail.Visible = page == Page.ConnectFailed;
            if (_disconnected != null) _disconnected.Visible = page == Page.Disconnected;
        }

        private enum Page : byte
        {
            Connecting,
            ConnectFailed,
            Disconnected,
        }
    }
}
