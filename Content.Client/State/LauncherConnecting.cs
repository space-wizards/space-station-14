using System;
using Content.Client.UserInterface;
using Robust.Client;
using Robust.Client.UserInterface;
using Robust.Shared.IoC;
using Robust.Shared.Network;

namespace Content.Client.State
{
    public class LauncherConnecting : Robust.Client.State.State
    {
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private readonly IClientNetManager _clientNetManager = default!;
        [Dependency] private readonly IGameController _gameController = default!;
        [Dependency] private readonly IBaseClient _baseClient = default!;

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

        public override void Startup()
        {
            _control = new LauncherConnectingGui(this);

            _userInterfaceManager.StateRoot.AddChild(_control);

            _clientNetManager.ConnectFailed += (_, args) =>
            {
                ConnectFailReason = args.Reason;
                CurrentPage = Page.ConnectFailed;
            };

            _clientNetManager.ClientConnectStateChanged += state => ConnectionStateChanged?.Invoke(state);

            CurrentPage = Page.Connecting;
        }

        public void RetryConnect()
        {
            if (_gameController.LaunchState.ConnectEndpoint != null)
            {
                _baseClient.ConnectToServer(_gameController.LaunchState.ConnectEndpoint);
                CurrentPage = Page.Connecting;
            }
        }

        public void Exit()
        {
            _gameController.Shutdown("Exit button pressed");
        }

        public override void Shutdown()
        {
            _control?.Dispose();
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
