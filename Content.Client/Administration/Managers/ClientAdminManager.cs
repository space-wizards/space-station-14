using System;
using System.Collections.Generic;
using System.Reflection;
using Content.Shared.Administration;
using Robust.Client.Console;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Reflection;

namespace Content.Client.Administration.Managers
{
    public sealed class ClientAdminManager : IClientAdminManager, IClientConGroupImplementation, IPostInjectInit
    {
        [Dependency] private readonly IClientNetManager _netMgr = default!;
        [Dependency] private readonly IClientConGroupController _conGroup = default!;

        private AdminData? _adminData;
        private readonly HashSet<string> _availableCommands = new();

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
        }

        private void UpdateMessageRx(MsgUpdateAdminStatus message)
        {
            _availableCommands.Clear();
            var host = IoCManager.Resolve<IClientConsoleHost>();

            // Anything marked as Any we'll just add even if the server doesn't know about it.
            foreach (var (command, instance) in host.RegisteredCommands)
            {
                if (Attribute.GetCustomAttribute(instance.GetType(), typeof(AnyCommandAttribute)) == null) continue;
                _availableCommands.Add(command);
            }

            _availableCommands.UnionWith(message.AvailableCommands);
            Logger.DebugS("admin", $"Have {message.AvailableCommands.Length} commands available");

            _adminData = message.Admin;
            if (_adminData != null)
            {
                var flagsText = string.Join("|", AdminFlagsHelper.FlagsToNames(_adminData.Flags));
                Logger.InfoS("admin", $"Updated admin status: {_adminData.Active}/{_adminData.Title}/{flagsText}");
            }
            else
            {
                Logger.InfoS("admin", "Updated admin status: Not admin");
            }

            AdminStatusUpdated?.Invoke();
            ConGroupUpdated?.Invoke();
        }

        public event Action? ConGroupUpdated;

        void IPostInjectInit.PostInject()
        {
            _conGroup.Implementation = this;
        }
    }
}
