using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Stylesheets;
using Content.Client.Utility;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Interfaces;
using Robust.Client.Interfaces.UserInterface;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using static Content.Client.StaticIoC;

namespace Content.Client.State
{
    public class LauncherConnecting : Robust.Client.State.State
    {
#pragma warning disable 649
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager;
        [Dependency] private readonly IStylesheetManager _stylesheetManager;
        [Dependency] private readonly IClientNetManager _clientNetManager;
        [Dependency] private readonly IGameController _gameController;
#pragma warning restore 649

        private Control _control;
        private Label _connectStatus;

        public override void Startup()
        {
            var panelTex = ResC.GetTexture("/Nano/button.svg.96dpi.png");
            var back = new StyleBoxTexture
            {
                Texture = panelTex,
                Modulate = new Color(32, 32, 48),
            };
            back.SetPatchMargin(StyleBox.Margin.All, 10);

            Button exitButton;
            Label connectFailReason;
            Control connectingStatus;

            var address = _gameController.LaunchState.Ss14Address ?? _gameController.LaunchState.ConnectAddress;

            _control = new Control
            {
                Stylesheet = _stylesheetManager.SheetSpace,
                Children =
                {
                    new PanelContainer
                    {
                        PanelOverride = back
                    },
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
                                                Children =
                                                {
                                                    (connectingStatus = new VBoxContainer
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
                                                    (connectFailReason = new Label
                                                    {
                                                        Align = Label.AlignMode.Center
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
                                                SizeFlagsVertical =
                                                    Control.SizeFlags.ShrinkEnd | Control.SizeFlags.Expand,
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

            _clientNetManager.ConnectFailed += (sender, args) =>
            {
                connectFailReason.Text = Loc.GetString("Failed to connect to server:\n{0}", args.Reason);
                connectingStatus.Visible = false;
                connectFailReason.Visible = true;
            };

            _clientNetManager.ClientConnectStateChanged += ConnectStateChanged;

            ConnectStateChanged(_clientNetManager.ClientConnectState);
        }

        private void ConnectStateChanged(ClientConnectionState state)
        {
            _connectStatus.Text = Loc.GetString(state switch
            {
                ClientConnectionState.NotConnecting => "Not connecting?",
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
    }
}
