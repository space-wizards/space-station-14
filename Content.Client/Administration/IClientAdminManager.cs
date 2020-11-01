using System;
using Content.Shared.Administration;

namespace Content.Client.Administration
{
    public interface IClientAdminManager
    {
        public event Action AdminStatusUpdated;

        bool HasFlag(AdminFlags flag);

        bool CanCommand(string cmdName);
        bool CanViewVar();
        bool CanAdminPlace();
        bool CanScript();
        bool CanAdminMenu();

        void Initialize();
    }
}
