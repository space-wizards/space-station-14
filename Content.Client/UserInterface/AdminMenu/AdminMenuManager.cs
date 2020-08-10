using Robust.Client.UserInterface.CustomControls;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Client.UserInterface.AdminMenu
{
    internal class AdminMenuManager : IAdminMenuManager
    {
        SS14Window _window;
        SS14Window _commandWindow; //TODO: make this a list/array of windows? then we can have multiple cmd windows open

        public void Initialize()
        {
            _window = new AdminMenuWindow();
        }

        public void Open()
        {
            //TODO: remove dis
            _window = new AdminMenuWindow();
            _window.OpenCentered();
        }

        public void Close()
        {
            _window.Close();
            _commandWindow.Close();
        }
    }

    internal interface IAdminMenuManager
    {
        void Initialize();
        void Open(); //TODO: remove this and import the pressed action (from IGameHud or somewhere else)
    }
}
