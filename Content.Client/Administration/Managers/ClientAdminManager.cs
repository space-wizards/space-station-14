using Content.Shared.Administration;
using Robust.Client.Console;
using Robust.Client.UserInterface;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Client.Administration.Managers
{
    public sealed class ClientAdminManager : SharedAdminManager, IClientAdminManager, IClientConGroupImplementation
    {
        [Dependency] private readonly IClientNetManager _netMgr = default!;
        [Dependency] private readonly IClientConGroupController _conGroup = default!;
        [Dependency] private readonly IUserInterfaceManager _userInterface = default!;

        private readonly AdminCommandPermissions _serverCommands = new();
        private AdminData? _adminData;

        public event Action? AdminStatusUpdated;
        public event Action? ConGroupUpdated;

        public override void Initialize()
        {
            base.Initialize();
            _netMgr.RegisterNetMessage<MsgUpdateAdminStatus>(UpdateMessageRx);
            _conGroup.Implementation = this;
        }

        public override void ReloadCommandPermissions()
        {
            base.ReloadCommandPermissions();

            if (ResMan.TryContentFileRead(new ResPath("/clientCommandPerms.yml"), out var efs))
                CommandPermissions.LoadPermissionsFromStream(efs);
        }

        public override void ReloadToolshedPermissions()
        {
            // base.ReloadToolshedPermissions();

            if (ResMan.TryContentFileRead(new ResPath("/clientToolshedEngineCommandPerms.yml"), out var f))
                ToolshedCommandPermissions.LoadPermissionsFromStream(f);
        }

        public bool IsActive()
        {
            return _adminData?.Active ?? false;
        }

        public bool HasFlag(AdminFlags flag)
        {
            return _adminData?.HasFlag(flag) ?? false;
        }

        public override bool CanCommand(ICommonSession session, string cmdName)
        {
            return PlayerMan.LocalUser == session.UserId
                ? CanCommand(cmdName)
                : base.CanCommand(session, cmdName);
        }

        public bool CanCommand(string cmdName)
        {
            if (_adminData != null && _adminData.HasFlag(AdminFlags.Host))
                return true;

            return CommandPermissions.CanCommand(cmdName, _adminData)
                   || _serverCommands.CanCommand(cmdName, _adminData);
        }

        public bool CanViewVar()
        {
            return CanCommand("vv");
        }

        public bool CanAdminPlace()
        {
            return _adminData?.CanAdminPlace() ?? false;
        }

        public bool CanScript()
        {
            return _adminData?.CanScript() ?? false;
        }

        public bool CanAdminMenu()
        {
            return _adminData?.CanAdminMenu() ?? false;
        }

        private void UpdateMessageRx(MsgUpdateAdminStatus message)
        {
            // The server sends us a list of commands we are currently allowed to execute.
            // It doesn't provide the full set of permission information.
            // Hence, we just bodge it and pretend that the server-side commands we are allowed to run actually have no
            // restrictions at all.
            _serverCommands.Clear();
            _serverCommands.AnyCommands.UnionWith(message.AvailableCommands);

            _adminData = message.Admin;
            if (_adminData != null)
            {
                var flagsText = string.Join("|", AdminFlagsHelper.FlagsToNames(_adminData.Flags));
                Log.Info($"Updated admin status: {_adminData.Active}/{_adminData.Title}/{flagsText}");

                if (_adminData.Active)
                    _userInterface.DebugMonitors.SetMonitor(DebugMonitor.Coords, true);
            }
            else
            {
                Log.Info("Updated admin status: Not admin");
            }

            Log.Debug($"Have {_serverCommands.AnyCommands.Count} server-side commands available");
            AdminStatusUpdated?.Invoke();
            ConGroupUpdated?.Invoke();
        }

        public override AdminData? GetAdminData(ICommonSession session, bool includeDeAdmin = false)
        {
            if (PlayerMan.LocalUser == session.UserId && (_adminData?.Active ?? includeDeAdmin))
                return _adminData;

            return null;
        }

        public AdminData? GetAdminData(bool includeDeAdmin = false)
        {
            if (PlayerMan.LocalSession is { } session)
                return GetAdminData(session, includeDeAdmin);

            return null;
        }
    }
}
