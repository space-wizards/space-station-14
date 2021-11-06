using System.Collections.Generic;
using Content.Client.Administration.UI;
using Content.Client.HUD;
using Content.Shared.Administration.Menu;
using Content.Shared.Input;
using Robust.Client.Console;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects;
using Robust.Shared.Input.Binding;
using Robust.Shared.IoC;
using Robust.Shared.Network;

namespace Content.Client.Administration.Managers
{
    internal class AdminMenuManager : IAdminMenuManager
    {
        [Dependency] private readonly INetManager _netManager = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IGameHud _gameHud = default!;
        [Dependency] private readonly IClientAdminManager _clientAdminManager = default!;
        [Dependency] private readonly IClientConGroupController _clientConGroupController = default!;
        [Dependency] private readonly IOverlayManager _overlayManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IEntityLookup _entityLookup = default!;

        private AdminMenuWindow? _window;
        private List<SS14Window> _commandWindows = new();
        private AdminNameOverlay _adminNameOverlay = default!;

        public void Initialize()
        {
            _netManager.RegisterNetMessage<AdminMenuPlayerListRequest>();
            _netManager.RegisterNetMessage<AdminMenuPlayerListMessage>(HandlePlayerListMessage);

            _commandWindows = new List<SS14Window>();
            _adminNameOverlay = new AdminNameOverlay(this, _entityManager, _eyeManager, _resourceCache, _entityLookup);
            // Reset the AdminMenu Window on disconnect
            _netManager.Disconnect += (_, _) => ResetWindow();

            _inputManager.SetInputCommand(ContentKeyFunctions.OpenAdminMenu,
                InputCmdHandler.FromDelegate(_ => Toggle()));

            _clientAdminManager.AdminStatusUpdated += () =>
            {
                // when status changes, show the top button if we can open admin menu.
                // if we can't or we lost admin status, close it and hide the button.
                _gameHud.AdminButtonVisible = CanOpen();
                if (!_gameHud.AdminButtonVisible)
                {
                    Close();
                }
            };
            _gameHud.AdminButtonToggled += (open) =>
            {
                if (open)
                {
                    TryOpen();
                }
                else
                {
                    Close();
                }
            };
            _gameHud.AdminButtonVisible = CanOpen();
            _gameHud.AdminButtonDown = false;
        }

        private void RequestPlayerList()
        {
            var message = _netManager.CreateNetMessage<AdminMenuPlayerListRequest>();

            _netManager.ClientSendMessage(message);
        }

        private void HandlePlayerListMessage(AdminMenuPlayerListMessage msg)
        {
            _window?.RefreshPlayerList(msg.PlayersInfo);
            _adminNameOverlay.UpdatePlayerInfo(msg.PlayersInfo);
        }

        private void AdminNameOverlayOn()
        {
            if (!_overlayManager.HasOverlay<AdminNameOverlay>())
                _overlayManager.AddOverlay(_adminNameOverlay);
        }

        private void AdminNameOverlayOff()
        {
            _overlayManager.RemoveOverlay<AdminNameOverlay>();
        }

        public void ResetWindow()
        {
            _window?.Close();
            _window = null;

            foreach (var window in _commandWindows)
                window?.Dispose();
            _commandWindows.Clear();
        }

        public void OpenCommand(SS14Window window)
        {
            _commandWindows.Add(window);
            window.OpenCentered();
        }

        public void Open()
        {
            _window ??= new AdminMenuWindow();
            _window.OnPlayerListRefresh += RequestPlayerList;
            _window.OnAdminNameOverlayOn += AdminNameOverlayOn;
            _window.OnAdminNameOverlayOff += AdminNameOverlayOff;
            _window.OpenCentered();
        }

        public void Close()
        {
            _window?.Close();

            foreach (var window in _commandWindows)
                window?.Dispose();
            _commandWindows.Clear();
        }

        /// <summary>
        /// Checks if the player can open the window
        /// </summary>
        /// <returns>True if the player is allowed</returns>
        public bool CanOpen()
        {
            return _clientConGroupController.CanAdminMenu();
        }

        /// <summary>
        /// Checks if the player can open the window and tries to open it
        /// </summary>
        public void TryOpen()
        {
            if (CanOpen())
                Open();
        }

        public void Toggle()
        {
            if (_window != null && _window.IsOpen)
            {
                Close();
            }
            else
            {
                TryOpen();
            }
        }
    }

    internal interface IAdminMenuManager
    {
        void Initialize();
        void Open();
        void OpenCommand(SS14Window window);
        bool CanOpen();
        void TryOpen();
        void Toggle();
    }
}
