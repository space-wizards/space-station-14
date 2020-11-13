using System.Collections.Generic;
using Content.Shared.Input;
using Robust.Client.Console;
using Robust.Client.Interfaces.Input;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Input.Binding;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;

namespace Content.Client.UserInterface.AdminMenu
{
    internal class AdminMenuManager : IAdminMenuManager
    {
        [Dependency] private readonly INetManager _netManager = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IClientConGroupController _clientConGroupController = default!;

        private SS14Window _window;
        private List<SS14Window> _commandWindows;

        public void Initialize()
        {
            _commandWindows = new List<SS14Window>();
            // Reset the AdminMenu Window on disconnect
            _netManager.Disconnect += (sender, channel) => ResetWindow();

            _inputManager.SetInputCommand(ContentKeyFunctions.OpenAdminMenu,
                InputCmdHandler.FromDelegate(session => Toggle()));
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
            if (_window == null)
                _window = new AdminMenuWindow();
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
