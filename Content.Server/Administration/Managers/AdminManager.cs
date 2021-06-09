using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Server.Interfaces.Chat;
using Content.Server.Players;
using Content.Shared;
using Content.Shared.Administration;
using Content.Shared.Administration.AdminMenu;
using Content.Shared.Network.NetMessages;
using Robust.Server.Console;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.Enums;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Network;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

#nullable enable

namespace Content.Server.Administration
{
    public sealed class AdminManager : IAdminManager, IPostInjectInit, IConGroupControllerImplementation
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IServerDbManager _dbManager = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IServerNetManager _netMgr = default!;
        [Dependency] private readonly IConGroupController _conGroup = default!;
        [Dependency] private readonly IResourceManager _res = default!;
        [Dependency] private readonly IServerConsoleHost _consoleHost = default!;
        [Dependency] private readonly IChatManager _chat = default!;

        private readonly Dictionary<IPlayerSession, AdminReg> _admins = new();
        private readonly HashSet<NetUserId> _promotedPlayers = new();

        public event Action<AdminPermsChangedEventArgs>? OnPermsChanged;

        public IEnumerable<IPlayerSession> ActiveAdmins => _admins
            .Where(p => p.Value.Data.Active)
            .Select(p => p.Key);

        // If a command isn't in this list it's server-console only.
        // if a command is in but the flags value is null it's available to everybody.
        private readonly HashSet<string> _anyCommands = new();
        private readonly Dictionary<string, AdminFlags[]> _adminCommands = new();

        public bool IsAdmin(IPlayerSession session, bool includeDeAdmin = false)
        {
            return GetAdminData(session, includeDeAdmin) != null;
        }

        public AdminData? GetAdminData(IPlayerSession session, bool includeDeAdmin = false)
        {
            if (_admins.TryGetValue(session, out var reg) && (reg.Data.Active || includeDeAdmin))
            {
                return reg.Data;
            }

            return null;
        }

        public void DeAdmin(IPlayerSession session)
        {
            if (!_admins.TryGetValue(session, out var reg))
            {
                throw new ArgumentException($"Player {session} is not an admin");
            }

            if (!reg.Data.Active)
            {
                return;
            }

            _chat.SendAdminAnnouncement(Loc.GetString("{0} de-adminned themselves.", session.Name));
            _chat.DispatchServerMessage(session, Loc.GetString("You are now a normal player."));

            var plyData = session.ContentData()!;
            plyData.ExplicitlyDeadminned = true;
            reg.Data.Active = false;

            SendPermsChangedEvent(session);
            UpdateAdminStatus(session);
        }

        public void ReAdmin(IPlayerSession session)
        {
            if (!_admins.TryGetValue(session, out var reg))
            {
                throw new ArgumentException($"Player {session} is not an admin");
            }

            _chat.DispatchServerMessage(session, Loc.GetString("You are now an admin."));

            var plyData = session.ContentData()!;
            plyData.ExplicitlyDeadminned = false;
            reg.Data.Active = true;

            _chat.SendAdminAnnouncement(Loc.GetString("{0} re-adminned themselves.", session.Name));

            SendPermsChangedEvent(session);
            UpdateAdminStatus(session);
        }

        public async void ReloadAdmin(IPlayerSession player)
        {
            var data = await LoadAdminData(player);
            var curAdmin = _admins.GetValueOrDefault(player);

            if (data == null && curAdmin == null)
            {
                // Wasn't admin before or after.
                return;
            }

            if (data == null)
            {
                // No longer admin.
                _admins.Remove(player);
                _chat.DispatchServerMessage(player, Loc.GetString("You are no longer an admin."));
            }
            else
            {
                var (aData, rankId, special) = data.Value;

                if (curAdmin == null)
                {
                    // Now an admin.
                    var reg = new AdminReg(player, aData)
                    {
                        IsSpecialLogin = special,
                        RankId = rankId
                    };
                    _admins.Add(player, reg);
                    _chat.DispatchServerMessage(player, Loc.GetString("You are now an admin."));
                }
                else
                {
                    // Perms changed.
                    curAdmin.IsSpecialLogin = special;
                    curAdmin.RankId = rankId;
                    curAdmin.Data = aData;
                }

                if (!player.ContentData()!.ExplicitlyDeadminned)
                {
                    aData.Active = true;

                    _chat.DispatchServerMessage(player, Loc.GetString("Your admin permissions have been updated."));
                }
            }

            SendPermsChangedEvent(player);
            UpdateAdminStatus(player);
        }

        public void ReloadAdminsWithRank(int rankId)
        {
            foreach (var dat in _admins.Values.Where(p => p.RankId == rankId).ToArray())
            {
                ReloadAdmin(dat.Session);
            }
        }

        public void Initialize()
        {
            _netMgr.RegisterNetMessage<MsgUpdateAdminStatus>(MsgUpdateAdminStatus.NAME);
            _netMgr.RegisterNetMessage<AdminMenuPlayerListRequest>(AdminMenuPlayerListRequest.NAME, HandlePlayerListRequest);
            _netMgr.RegisterNetMessage<AdminMenuPlayerListMessage>(AdminMenuPlayerListMessage.NAME);

            // Cache permissions for loaded console commands with the requisite attributes.
            foreach (var (cmdName, cmd) in _consoleHost.RegisteredCommands)
            {
                var (isAvail, flagsReq) = GetRequiredFlag(cmd);

                if (!isAvail)
                {
                    continue;
                }

                if (flagsReq.Length != 0)
                {
                    _adminCommands.Add(cmdName, flagsReq);
                }
                else
                {
                    _anyCommands.Add(cmdName);
                }
            }

            // Load flags for engine commands, since those don't have the attributes.
            if (_res.TryContentFileRead(new ResourcePath("/engineCommandPerms.yml"), out var fs))
            {
                using var reader = new StreamReader(fs, EncodingHelpers.UTF8);
                var yStream = new YamlStream();
                yStream.Load(reader);
                var root = (YamlSequenceNode) yStream.Documents[0].RootNode;

                foreach (var child in root)
                {
                    var map = (YamlMappingNode) child;
                    var commands = map.GetNode<YamlSequenceNode>("Commands").Select(p => p.AsString());
                    if (map.TryGetNode("Flags", out var flagsNode))
                    {
                        var flagNames = flagsNode.AsString().Split(",", StringSplitOptions.RemoveEmptyEntries);
                        var flags = AdminFlagsHelper.NamesToFlags(flagNames);
                        foreach (var cmd in commands)
                        {
                            if (!_adminCommands.TryGetValue(cmd, out var exFlags))
                            {
                                _adminCommands.Add(cmd, new[] {flags});
                            }
                            else
                            {
                                var newArr = new AdminFlags[exFlags.Length + 1];
                                exFlags.CopyTo(newArr, 0);
                                exFlags[^1] = flags;
                                _adminCommands[cmd] = newArr;
                            }
                        }
                    }
                    else
                    {
                        _anyCommands.UnionWith(commands);
                    }
                }
            }
        }

        private void HandlePlayerListRequest(AdminMenuPlayerListRequest message)
        {
            var senderSession = _playerManager.GetSessionByChannel(message.MsgChannel);

            if (!_admins.ContainsKey(senderSession))
            {
                return;
            }

            var netMsg = _netMgr.CreateNetMessage<AdminMenuPlayerListMessage>();

            netMsg.PlayersInfo.Clear();

            foreach (var session in _playerManager.GetAllPlayers())
            {
                var name = session.Name;
                var username = session.AttachedEntity?.Name ?? "";
                var antag = session.ContentData()?.Mind?.AllRoles.Any(r => r.Antagonist) ?? false;

                netMsg.PlayersInfo.Add(new AdminMenuPlayerListMessage.PlayerInfo(name, username, antag));
            }

            _netMgr.ServerSendMessage(netMsg, senderSession.ConnectedClient);
        }

        public void PromoteHost(IPlayerSession player)
        {
            _promotedPlayers.Add(player.UserId);

            ReloadAdmin(player);
        }

        void IPostInjectInit.PostInject()
        {
            _playerManager.PlayerStatusChanged += PlayerStatusChanged;
            _conGroup.Implementation = this;
        }

        // NOTE: Also sends commands list for non admins..
        private void UpdateAdminStatus(IPlayerSession session)
        {
            var msg = _netMgr.CreateNetMessage<MsgUpdateAdminStatus>();

            var commands = new List<string>(_anyCommands);

            if (_admins.TryGetValue(session, out var adminData))
            {
                msg.Admin = adminData.Data;

                commands.AddRange(_adminCommands
                    .Where(p => p.Value.Any(f => adminData.Data.HasFlag(f)))
                    .Select(p => p.Key));
            }

            msg.AvailableCommands = commands.ToArray();

            _netMgr.ServerSendMessage(msg, session.ConnectedClient);
        }

        private void PlayerStatusChanged(object? sender, SessionStatusEventArgs e)
        {
            if (e.NewStatus == SessionStatus.Connected)
            {
                // Run this so that available commands list gets sent.
                UpdateAdminStatus(e.Session);
            }
            else if (e.NewStatus == SessionStatus.InGame)
            {
                LoginAdminMaybe(e.Session);
            }
            else if (e.NewStatus == SessionStatus.Disconnected)
            {
                if (_admins.Remove(e.Session) && _cfg.GetCVar(CCVars.AdminAnnounceLogout))
                {
                    _chat.SendAdminAnnouncement(Loc.GetString("Admin logout: {0}", e.Session.Name));
                }
            }
        }

        private async void LoginAdminMaybe(IPlayerSession session)
        {
            var adminDat = await LoadAdminData(session);
            if (adminDat == null)
            {
                // Not an admin.
                return;
            }

            var (dat, rankId, specialLogin) = adminDat.Value;
            var reg = new AdminReg(session, dat)
            {
                IsSpecialLogin = specialLogin,
                RankId = rankId
            };

            _admins.Add(session, reg);

            if (!session.ContentData()!.ExplicitlyDeadminned)
            {
                reg.Data.Active = true;

                if (_cfg.GetCVar(CCVars.AdminAnnounceLogin))
                {
                    _chat.SendAdminAnnouncement(Loc.GetString("Admin login: {0}", session.Name));
                }

                SendPermsChangedEvent(session);
            }

            UpdateAdminStatus(session);
        }

        private async Task<(AdminData dat, int? rankId, bool specialLogin)?> LoadAdminData(IPlayerSession session)
        {
            if (IsLocal(session) && _cfg.GetCVar(CCVars.ConsoleLoginLocal) || _promotedPlayers.Contains(session.UserId))
            {
                var data = new AdminData
                {
                    Title = Loc.GetString("Host"),
                    Flags = AdminFlagsHelper.Everything,
                };

                return (data, null, true);
            }
            else
            {
                var dbData = await _dbManager.GetAdminDataForAsync(session.UserId);

                if (dbData == null)
                {
                    // Not an admin!
                    return null;
                }

                var flags = AdminFlags.None;

                if (dbData.AdminRank != null)
                {
                    flags = AdminFlagsHelper.NamesToFlags(dbData.AdminRank.Flags.Select(p => p.Flag));
                }

                foreach (var dbFlag in dbData.Flags)
                {
                    var flag = AdminFlagsHelper.NameToFlag(dbFlag.Flag);
                    if (dbFlag.Negative)
                    {
                        flags &= ~flag;
                    }
                    else
                    {
                        flags |= flag;
                    }
                }

                var data = new AdminData
                {
                    Flags = flags
                };

                if (dbData.Title != null)
                {
                    data.Title = dbData.Title;
                }
                else if (dbData.AdminRank != null)
                {
                    data.Title = dbData.AdminRank.Name;
                }

                return (data, dbData.AdminRankId, false);
            }
        }

        private static bool IsLocal(IPlayerSession player)
        {
            var ep = player.ConnectedClient.RemoteEndPoint;
            var addr = ep.Address;
            if (addr.IsIPv4MappedToIPv6)
            {
                addr = addr.MapToIPv4();
            }

            return Equals(addr, IPAddress.Loopback) || Equals(addr, IPAddress.IPv6Loopback);
        }

        public bool CanCommand(IPlayerSession session, string cmdName)
        {
            if (_anyCommands.Contains(cmdName))
            {
                // Anybody can use this command.
                return true;
            }

            if (!_adminCommands.TryGetValue(cmdName, out var flagsReq))
            {
                // Server-console only.
                return false;
            }

            var data = GetAdminData(session);
            if (data == null)
            {
                // Player isn't an admin.
                return false;
            }

            foreach (var flagReq in flagsReq)
            {
                if (data.HasFlag(flagReq))
                {
                    return true;
                }
            }

            return false;
        }

        private static (bool isAvail, AdminFlags[] flagsReq) GetRequiredFlag(IConsoleCommand cmd)
        {
            var type = cmd.GetType();
            if (Attribute.IsDefined(type, typeof(AnyCommandAttribute)))
            {
                // Available to everybody.
                return (true, Array.Empty<AdminFlags>());
            }

            var attribs = type.GetCustomAttributes(typeof(AdminCommandAttribute))
                .Cast<AdminCommandAttribute>()
                .Select(p => p.Flags)
                .ToArray();

            // If attribs.length == 0 then no access attribute is specified,
            // and this is a server-only command.
            return (attribs.Length != 0, attribs);
        }

        public bool CanViewVar(IPlayerSession session)
        {
            return GetAdminData(session)?.CanViewVar() ?? false;
        }

        public bool CanAdminPlace(IPlayerSession session)
        {
            return GetAdminData(session)?.CanAdminPlace() ?? false;
        }

        public bool CanScript(IPlayerSession session)
        {
            return GetAdminData(session)?.CanScript() ?? false;
        }

        public bool CanAdminMenu(IPlayerSession session)
        {
            return GetAdminData(session)?.CanAdminMenu() ?? false;
        }

        public bool CanAdminReloadPrototypes(IPlayerSession session)
        {
            return GetAdminData(session)?.CanAdminReloadPrototypes() ?? false;
        }

        private void SendPermsChangedEvent(IPlayerSession session)
        {
            var flags = GetAdminData(session)?.Flags;
            OnPermsChanged?.Invoke(new AdminPermsChangedEventArgs(session, flags));
        }

        private sealed class AdminReg
        {
            public readonly IPlayerSession Session;

            public AdminData Data;
            public int? RankId;

            // Such as console.loginlocal or promotehost
            public bool IsSpecialLogin;

            public AdminReg(IPlayerSession session, AdminData data)
            {
                Data = data;
                Session = session;
            }
        }
    }
}
