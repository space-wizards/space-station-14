using System;
using System.Collections.Generic;
using Content.Shared.Administration;
using Content.Shared.Network.NetMessages;
using Robust.Client.Console;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network;

#nullable enable

namespace Content.Client.Administration
{
    public class ClientAdminManager : IClientAdminManager, IClientConGroupImplementation, IPostInjectInit
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
            return _adminData?.CanViewVar() ?? false;
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
            _netMgr.RegisterNetMessage<MsgUpdateAdminStatus>(MsgUpdateAdminStatus.NAME, UpdateMessageRx);
        }

        private void UpdateMessageRx(MsgUpdateAdminStatus message)
        {
            _availableCommands.Clear();
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
