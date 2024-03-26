using System;
using Robust.Client;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Client.Launcher
{
    public sealed class LauncherConnecting : Robust.Client.State.State
    {
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private readonly IClientNetManager _clientNetManager = default!;
        [Dependency] private readonly IGameController _gameController = default!;
        [Dependency] private readonly IBaseClient _baseClient = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        private LauncherConnectingGui? _control;

        private Page _currentPage;
        private string? _connectFailReason;

        public string? Address => _gameController.LaunchState.Ss14Address ?? _gameController.LaunchState.ConnectAddress;

        public string? ConnectFailReason
        {
            get => _connectFailReason;
            private set
            {
                _connectFailReason = value;
                ConnectFailReasonChanged?.Invoke(value);
            }
        }

        public string? LastDisconnectReason => _baseClient.LastDisconnectReason;

        public Page CurrentPage
        {
            get => _currentPage;
            private set
            {
                _currentPage = value;
                PageChanged?.Invoke(value);
            }
        }

        public ClientConnectionState ConnectionState => _clientNetManager.ClientConnectState;

        public event Action<Page>? PageChanged;
        public event Action<string?>? ConnectFailReasonChanged;
        public event Action<ClientConnectionState>? ConnectionStateChanged;
        public event Action<NetConnectFailArgs>? ConnectFailed;

        protected override void Startup()
        {
            _control = new LauncherConnectingGui(this, _random, _prototypeManager, _cfg);

            _userInterfaceManager.StateRoot.AddChild(_control);

            _clientNetManager.ConnectFailed += OnConnectFailed;
            _clientNetManager.ClientConnectStateChanged += OnConnectStateChanged;

            CurrentPage = Page.Connecting;
        }

        protected override void Shutdown()
        {
            _control?.Dispose();

            _clientNetManager.ConnectFailed -= OnConnectFailed;
            _clientNetManager.ClientConnectStateChanged -= OnConnectStateChanged;
        }

        private void OnConnectFailed(object? _, NetConnectFailArgs args)
        {
            if (args.RedialFlag)
            {
                // We've just *attempted* to connect and we've been told we need to redial, so do it.
                // Result deliberately discarded.
                Redial();
            }
            ConnectFailReason = args.Reason;
            CurrentPage = Page.ConnectFailed;
            ConnectFailed?.Invoke(args);
        }

        private void OnConnectStateChanged(ClientConnectionState state)
        {
            ConnectionStateChanged?.Invoke(state);
        }

        public void RetryConnect()
        {
            if (_gameController.LaunchState.ConnectEndpoint != null)
            {
                _baseClient.ConnectToServer(_gameController.LaunchState.ConnectEndpoint);
                CurrentPage = Page.Connecting;
            }
        }

        public bool Redial()
        {
            try
            {
                if (_gameController.LaunchState.Ss14Address != null)
                {
                    _gameController.Redial(_gameController.LaunchState.Ss14Address);
                    return true;
                }
                else
                {
                    Logger.InfoS("launcher-ui", $"Redial not possible, no Ss14Address");
                }
            }
            catch (Exception ex)
            {
                Logger.ErrorS("launcher-ui", $"Redial exception: {ex}");
            }
            return false;
        }

        public void Exit()
        {
            _gameController.Shutdown("Exit button pressed");
        }

        public void SetDisconnected()
        {
            CurrentPage = Page.Disconnected;
        }

        public enum Page : byte
        {
            Connecting,
            ConnectFailed,
            Disconnected,
        }
    }
}
