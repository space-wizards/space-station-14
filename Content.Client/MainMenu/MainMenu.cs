using System;
using System.Text.RegularExpressions;
using Content.Client.Changelog;
using Content.Client.UserInterface;
using Robust.Client;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Utility;
using UsernameHelpers = Robust.Shared.AuthLib.UsernameHelpers;

namespace Content.Client.State
{
    /// <summary>
    ///     Main menu screen that is the first screen to be displayed when the game starts.
    /// </summary>
    // Instantiated dynamically through the StateManager, Dependencies will be resolved.
    public class MainScreen : Robust.Client.State.State
    {
        private const string PublicServerAddress = "server.spacestation14.io";

        [Dependency] private readonly IBaseClient _client = default!;
        [Dependency] private readonly IClientNetManager _netManager = default!;
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;
        [Dependency] private readonly IGameController _controllerProxy = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;

        private MainMenuControl _mainMenuControl = default!;
        private OptionsMenu _optionsMenu = default!;
        private bool _isConnecting;

        // ReSharper disable once InconsistentNaming
        private static readonly Regex IPv6Regex = new(@"\[(.*:.*:.*)](?::(\d+))?");

        /// <inheritdoc />
        public override void Startup()
        {
            _mainMenuControl = new MainMenuControl(_resourceCache, _configurationManager);
            _userInterfaceManager.StateRoot.AddChild(_mainMenuControl);

            _mainMenuControl.QuitButton.OnPressed += QuitButtonPressed;
            _mainMenuControl.OptionsButton.OnPressed += OptionsButtonPressed;
            _mainMenuControl.DirectConnectButton.OnPressed += DirectConnectButtonPressed;
            _mainMenuControl.JoinPublicServerButton.OnPressed += JoinPublicServerButtonPressed;
            _mainMenuControl.AddressBox.OnTextEntered += AddressBoxEntered;

            _client.RunLevelChanged += RunLevelChanged;

            _optionsMenu = new OptionsMenu();
        }

        /// <inheritdoc />
        public override void Shutdown()
        {
            _client.RunLevelChanged -= RunLevelChanged;
            _netManager.ConnectFailed -= _onConnectFailed;

            _mainMenuControl.Dispose();
            _optionsMenu.Dispose();
        }

        private void QuitButtonPressed(BaseButton.ButtonEventArgs args)
        {
            _controllerProxy.Shutdown();
        }

        private void OptionsButtonPressed(BaseButton.ButtonEventArgs args)
        {
            _optionsMenu.OpenCentered();
        }

        private void DirectConnectButtonPressed(BaseButton.ButtonEventArgs args)
        {
            var input = _mainMenuControl.AddressBox;
            TryConnect(input.Text);
        }

        private void JoinPublicServerButtonPressed(BaseButton.ButtonEventArgs args)
        {
            TryConnect(PublicServerAddress);
        }

        private void AddressBoxEntered(LineEdit.LineEditEventArgs args)
        {
            if (_isConnecting)
            {
                return;
            }

            TryConnect(args.Text);
        }

        private void TryConnect(string address)
        {
            var inputName = _mainMenuControl.UserNameBox.Text.Trim();
            if (!UsernameHelpers.IsNameValid(inputName, out var reason))
            {
                var invalidReason = Loc.GetString(reason.ToText());
                _userInterfaceManager.Popup(
                    Loc.GetString("Invalid username:\n{0}", invalidReason),
                    Loc.GetString("Invalid Username"));
                return;
            }

            var configName = _configurationManager.GetCVar(CVars.PlayerName);
            if (_mainMenuControl.UserNameBox.Text != configName)
            {
                _configurationManager.SetCVar(CVars.PlayerName, inputName);
                _configurationManager.SaveToFile();
            }

            _setConnectingState(true);
            _netManager.ConnectFailed += _onConnectFailed;
            try
            {
                ParseAddress(address, out var ip, out var port);
                _client.ConnectToServer(ip, port);
            }
            catch (ArgumentException e)
            {
                _userInterfaceManager.Popup($"Unable to connect: {e.Message}", "Connection error.");
                Logger.Warning(e.ToString());
                _netManager.ConnectFailed -= _onConnectFailed;
                _setConnectingState(false);
            }
        }

        private void RunLevelChanged(object? obj, RunLevelChangedEventArgs args)
        {
            if (args.NewLevel == ClientRunLevel.Initialize)
            {
                _setConnectingState(false);
                _netManager.ConnectFailed -= _onConnectFailed;
            }
        }

        private void ParseAddress(string address, out string ip, out ushort port)
        {
            var match6 = IPv6Regex.Match(address);
            if (match6 != Match.Empty)
            {
                ip = match6.Groups[1].Value;
                if (!match6.Groups[2].Success)
                {
                    port = _client.DefaultPort;
                }
                else if (!ushort.TryParse(match6.Groups[2].Value, out port))
                {
                    throw new ArgumentException("Not a valid port.");
                }

                return;
            }

            // See if the IP includes a port.
            var split = address.Split(':');
            ip = address;
            port = _client.DefaultPort;
            if (split.Length > 2)
            {
                throw new ArgumentException("Not a valid Address.");
            }

            // IP:port format.
            if (split.Length == 2)
            {
                ip = split[0];
                if (!ushort.TryParse(split[1], out port))
                {
                    throw new ArgumentException("Not a valid port.");
                }
            }
        }

        private void _onConnectFailed(object? _, NetConnectFailArgs args)
        {
            _userInterfaceManager.Popup($"Failed to connect:\n{args.Reason}");
            _netManager.ConnectFailed -= _onConnectFailed;
            _setConnectingState(false);
        }

        private void _setConnectingState(bool state)
        {
            _isConnecting = state;
            _mainMenuControl.DirectConnectButton.Disabled = state;
#if FULL_RELEASE
            _mainMenuControl.JoinPublicServerButton.Disabled = state;
#endif
        }

        private sealed class MainMenuControl : Control
        {
            private readonly IResourceCache _resourceCache;
            private readonly IConfigurationManager _configurationManager;

            public LineEdit UserNameBox { get; }
            public Button JoinPublicServerButton { get; }
            public LineEdit AddressBox { get; }
            public Button DirectConnectButton { get; }
            public Button OptionsButton { get; }
            public Button QuitButton { get; }
            public Label VersionLabel { get; }

            public MainMenuControl(IResourceCache resCache, IConfigurationManager configMan)
            {
                _resourceCache = resCache;
                _configurationManager = configMan;

                LayoutContainer.SetAnchorPreset(this, LayoutContainer.LayoutPreset.Wide);

                AddChild(new ParallaxControl());

                var layout = new LayoutContainer();
                AddChild(layout);

                var vBox = new VBoxContainer
                {
                    StyleIdentifier = "mainMenuVBox"
                };

                layout.AddChild(vBox);
                LayoutContainer.SetAnchorPreset(vBox, LayoutContainer.LayoutPreset.TopRight);
                LayoutContainer.SetMarginRight(vBox, -25);
                LayoutContainer.SetMarginTop(vBox, 30);
                LayoutContainer.SetGrowHorizontal(vBox, LayoutContainer.GrowDirection.Begin);

                var logoTexture = _resourceCache.GetResource<TextureResource>("/Textures/Logo/logo.png");
                var logo = new TextureRect
                {
                    Texture = logoTexture,
                    Stretch = TextureRect.StretchMode.KeepCentered,
                };
                vBox.AddChild(logo);

                var userNameHBox = new HBoxContainer {SeparationOverride = 4};
                vBox.AddChild(userNameHBox);
                userNameHBox.AddChild(new Label {Text = "Username:"});

                var currentUserName = _configurationManager.GetCVar(CVars.PlayerName);
                UserNameBox = new LineEdit
                {
                    Text = currentUserName, PlaceHolder = "Username",
                    HorizontalExpand = true
                };

                userNameHBox.AddChild(UserNameBox);

                JoinPublicServerButton = new Button
                {
                    Text = "Join Public Server",
                    StyleIdentifier = "mainMenu",
                    TextAlign = Label.AlignMode.Center,
#if !FULL_RELEASE
                    Disabled = true,
                    ToolTip = "Cannot connect to public server with a debug build."
#endif
                };

                vBox.AddChild(JoinPublicServerButton);

                // Separator.
                vBox.AddChild(new Control {MinSize = (0, 2)});

                AddressBox = new LineEdit
                {
                    Text = "localhost",
                    PlaceHolder = "server address:port",
                    HorizontalExpand = true
                };

                vBox.AddChild(AddressBox);

                DirectConnectButton = new Button
                {
                    Text = "Direct Connect",
                    TextAlign = Label.AlignMode.Center,
                    StyleIdentifier = "mainMenu",
                };

                vBox.AddChild(DirectConnectButton);

                // Separator.
                vBox.AddChild(new Control {MinSize = (0, 2)});

                OptionsButton = new Button
                {
                    Text = "Options",
                    TextAlign = Label.AlignMode.Center,
                    StyleIdentifier = "mainMenu",
                };

                vBox.AddChild(OptionsButton);

                QuitButton = new Button
                {
                    Text = "Quit",
                    TextAlign = Label.AlignMode.Center,
                    StyleIdentifier = "mainMenu",
                };

                vBox.AddChild(QuitButton);

                vBox.AddChild(new ChangelogButton());

                VersionLabel = new Label
                {
                    Text = "v0.1"
                };

                LayoutContainer.SetAnchorPreset(VersionLabel, LayoutContainer.LayoutPreset.BottomRight);
                LayoutContainer.SetGrowHorizontal(VersionLabel, LayoutContainer.GrowDirection.Begin);
                LayoutContainer.SetGrowVertical(VersionLabel, LayoutContainer.GrowDirection.Begin);
                layout.AddChild(VersionLabel);
            }
        }
    }
}
