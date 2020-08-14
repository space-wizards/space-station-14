using Robust.Client.Console;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;

namespace Content.Client.UserInterface.AdminMenu
{
    internal class AdminMenuManager : IAdminMenuManager
    {
#pragma warning disable 649
        [Dependency] private INetManager _netManager = default!;
#pragma warning restore 649
        private SS14Window _window;
        private SS14Window _commandWindow; //TODO EXP: make this a list/array of windows? then we can have multiple cmd windows open

        public void Initialize() 
        {
            // Reset the AdminMenu Window on disconnect
            _netManager.Disconnect += (sender, channel) => ResetWindow();
        }

        public void ResetWindow()
        {
            _window?.Close();
            _window = null;
        }

        public void OpenCommand(SS14Window window)
        {
            _commandWindow = window;
            _commandWindow.OpenCentered();
        }

        public void Open()
        {
            if (_window == null)
                _window = new AdminMenuWindow();
            _window.OpenCentered();
        }

        public void Close()
        {
            _window.Close();
            _commandWindow.Close();
        }

        public bool CanOpen()
        {
            return IoCManager.Resolve<IClientConGroupController>().CanAdminMenu();
        }
    }

    internal interface IAdminMenuManager
    {
        void Initialize();
        void Open(); //TODO EXP: remove this and import the pressed action (from IGameHud or somewhere else)
        void OpenCommand(SS14Window window);
        bool CanOpen();
    }
}
