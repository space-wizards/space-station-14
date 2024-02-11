using Content.Shared.Administration;
using Content.Shared.Administration.Managers;
using Robust.Client.Console;
using Robust.Client.Player;
using Robust.Shared.ContentPack;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Client.Administration.Managers
{
    public sealed class ClientAdminManager : IClientAdminManager, IClientConGroupImplementation, IPostInjectInit, ISharedAdminManager
    {
        [Dependency] private readonly IPlayerManager _player = default!;
        [Dependency] private readonly IClientNetManager _netMgr = default!;
        [Dependency] private readonly IClientConGroupController _conGroup = default!;
        [Dependency] private readonly IResourceManager _res = default!;
        [Dependency] private readonly ILogManager _logManager = default!;

        private AdminData? _adminData;
        private readonly HashSet<string> _availableCommands = new();

        private readonly AdminCommandPermissions _localCommandPermissions = new();
        private ISawmill _sawmill = default!;

        public event Action? AdminStatusUpdated;

        public bool IsActive()
        {
            return _adminData?.Active ?? false;
        }

        public bool HasFlag(AdminFlags flag)
        {
            return _adminData?.HasFlag(flag) ?? false;
        }

        public bool CanCommand(string cmdName)
        {
            if (_adminData != null && _adminData.HasFlag(AdminFlags.Host))
            {
                // Host can execute all commands when connected.
                // Kind of a shortcut to avoid pains during development.
                return true;
            }

            if (_localCommandPermissions.CanCommand(cmdName, _adminData))
                return true;

            return _availableCommands.Contains(cmdName);
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

        public void Initialize()
        {
            _netMgr.RegisterNetMessage<MsgUpdateAdminStatus>(UpdateMessageRx);

            // Load flags for engine commands, since those don't have the attributes.
            if (_res.TryContentFileRead(new ResPath("/clientCommandPerms.yml"), out var efs))
            {
                _localCommandPermissions.LoadPermissionsFromStream(efs);
            }
        }

        private void UpdateMessageRx(MsgUpdateAdminStatus message)
        {
            _availableCommands.Clear();
            var host = IoCManager.Resolve<IClientConsoleHost>();

            // Anything marked as Any we'll just add even if the server doesn't know about it.
            foreach (var (command, instance) in host.AvailableCommands)
            {
                if (Attribute.GetCustomAttribute(instance.GetType(), typeof(AnyCommandAttribute)) == null) continue;
                _availableCommands.Add(command);
            }

            _availableCommands.UnionWith(message.AvailableCommands);
            _sawmill.Debug($"Have {message.AvailableCommands.Length} commands available");

            _adminData = message.Admin;
            if (_adminData != null)
            {
                var flagsText = string.Join("|", AdminFlagsHelper.FlagsToNames(_adminData.Flags));
                _sawmill.Info($"Updated admin status: {_adminData.Active}/{_adminData.Title}/{flagsText}");
            }
            else
            {
                _sawmill.Info("Updated admin status: Not admin");
            }

            AdminStatusUpdated?.Invoke();
            ConGroupUpdated?.Invoke();
        }

        public event Action? ConGroupUpdated;

        void IPostInjectInit.PostInject()
        {
            _conGroup.Implementation = this;
            _sawmill = _logManager.GetSawmill("admin");
        }

        public AdminData? GetAdminData(EntityUid uid, bool includeDeAdmin = false)
        {
            return uid == _player.LocalEntity ? _adminData : null;
        }

        public AdminData? GetAdminData(ICommonSession session, bool includeDeAdmin = false)
        {
            if (_player.LocalUser == session.UserId)
                return _adminData;

            return null;
        }

        public AdminData? GetAdminData(bool includeDeAdmin = false)
        {
            if (_player.LocalSession is { } session)
                return GetAdminData(session, includeDeAdmin);

            return null;
        }
    }
}
