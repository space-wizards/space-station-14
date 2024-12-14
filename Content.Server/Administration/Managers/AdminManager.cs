using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Content.Server.Chat.Managers;
using Content.Server.Database;
using Content.Server.Players;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Info;
using Content.Shared.Players;
using Robust.Server.Console;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Utility;


namespace Content.Server.Administration.Managers
{
    public sealed partial class AdminManager : IAdminManager, IPostInjectInit, IConGroupControllerImplementation
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IServerDbManager _dbManager = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IServerNetManager _netMgr = default!;
        [Dependency] private readonly IConGroupController _conGroup = default!;
        [Dependency] private readonly IResourceManager _res = default!;
        [Dependency] private readonly IServerConsoleHost _consoleHost = default!;
        [Dependency] private readonly IChatManager _chat = default!;
        [Dependency] private readonly ToolshedManager _toolshed = default!;
        [Dependency] private readonly ILogManager _logManager = default!;

        private readonly Dictionary<ICommonSession, AdminReg> _admins = new();
        private readonly HashSet<NetUserId> _promotedPlayers = new();

        public event Action<AdminPermsChangedEventArgs>? OnPermsChanged;

        public IEnumerable<ICommonSession> ActiveAdmins => _admins
            .Where(p => p.Value.Data.Active)
            .Select(p => p.Key);

        public IEnumerable<ICommonSession> AllAdmins => _admins.Select(p => p.Key);

        private readonly AdminCommandPermissions _commandPermissions = new();
        private readonly AdminCommandPermissions _toolshedCommandPermissions = new();

        private ISawmill _sawmill = default!;

        public bool IsAdmin(ICommonSession session, bool includeDeAdmin = false)
        {
            return GetAdminData(session, includeDeAdmin) != null;
        }

        public AdminData? GetAdminData(ICommonSession session, bool includeDeAdmin = false)
        {
            if (_admins.TryGetValue(session, out var reg) && (reg.Data.Active || includeDeAdmin))
            {
                return reg.Data;
            }

            return null;
        }

        public AdminData? GetAdminData(EntityUid uid, bool includeDeAdmin = false)
        {
            if (_playerManager.TryGetSessionByEntity(uid, out var session))
                return GetAdminData(session, includeDeAdmin);

            return null;
        }

        public void DeAdmin(ICommonSession session)
        {
            if (!_admins.TryGetValue(session, out var reg))
            {
                throw new ArgumentException($"Player {session} is not an admin");
            }

            if (!reg.Data.Active)
            {
                return;
            }

            _chat.SendAdminAnnouncement(Loc.GetString("admin-manager-self-de-admin-message", ("exAdminName", session.Name)));
            _chat.DispatchServerMessage(session, Loc.GetString("admin-manager-became-normal-player-message"));

            var plyData = session.ContentData()!;
            plyData.ExplicitlyDeadminned = true;
            reg.Data.Active = false;

            SendPermsChangedEvent(session);
            UpdateAdminStatus(session);
        }

        public void Stealth(ICommonSession session)
        {
            if (!_admins.TryGetValue(session, out var reg))
            {
                throw new ArgumentException($"Player {session} is not an admin");
            }

            if (reg.Data.Stealth)
                return;

            var playerData = session.ContentData()!;
            playerData.Stealthed = true;
            reg.Data.Stealth = true;

            _chat.DispatchServerMessage(session, Loc.GetString("admin-manager-stealthed-message"));
            _chat.SendAdminAnnouncement(Loc.GetString("admin-manager-self-de-admin-message", ("exAdminName", session.Name)), AdminFlags.Stealth);
            _chat.SendAdminAnnouncement(Loc.GetString("admin-manager-self-enable-stealth", ("stealthAdminName", session.Name)), flagWhitelist: AdminFlags.Stealth);
        }

        public void UnStealth(ICommonSession session)
        {
            if (!_admins.TryGetValue(session, out var reg))
            {
                throw new ArgumentException($"Player {session} is not an admin");
            }

            if (!reg.Data.Stealth)
                return;

            var playerData = session.ContentData()!;
            playerData.Stealthed = false;
            reg.Data.Stealth = false;

            _chat.DispatchServerMessage(session, Loc.GetString("admin-manager-unstealthed-message"));
            _chat.SendAdminAnnouncement(Loc.GetString("admin-manager-self-re-admin-message", ("newAdminName", session.Name)), flagBlacklist: AdminFlags.Stealth);
            _chat.SendAdminAnnouncement(Loc.GetString("admin-manager-self-disable-stealth", ("exStealthAdminName", session.Name)), flagWhitelist: AdminFlags.Stealth);
        }

        public void ReAdmin(ICommonSession session)
        {
            if (!_admins.TryGetValue(session, out var reg))
            {
                throw new ArgumentException($"Player {session} is not an admin");
            }

            if (reg.Data.Active)
            {
                return;
            }

            _chat.DispatchServerMessage(session, Loc.GetString("admin-manager-became-admin-message"));

            var plyData = session.ContentData()!;
            plyData.ExplicitlyDeadminned = false;
            reg.Data.Active = true;

            if (!reg.Data.Stealth)
            {
                _chat.SendAdminAnnouncement(Loc.GetString("admin-manager-self-re-admin-message", ("newAdminName", session.Name)));
            }
            else
            {
                _chat.DispatchServerMessage(session, Loc.GetString("admin-manager-stealthed-message"));
                _chat.SendAdminAnnouncement(Loc.GetString("admin-manager-self-re-admin-message",
                    ("newAdminName", session.Name)), flagWhitelist: AdminFlags.Stealth);
            }

            SendPermsChangedEvent(session);
            UpdateAdminStatus(session);
        }

        public async void ReloadAdmin(ICommonSession player)
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
                _chat.DispatchServerMessage(player, Loc.GetString("admin-manager-no-longer-admin-message"));
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
                    _chat.DispatchServerMessage(player, Loc.GetString("admin-manager-became-admin-message"));
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

                    _chat.DispatchServerMessage(player, Loc.GetString("admin-manager-admin-permissions-updated-message"));
                }

                if (player.ContentData()!.Stealthed)
                {
                    aData.Stealth = true;
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
            _sawmill = _logManager.GetSawmill("admin");

            _netMgr.RegisterNetMessage<MsgUpdateAdminStatus>();

            // Cache permissions for loaded console commands with the requisite attributes.
            foreach (var (cmdName, cmd) in _consoleHost.AvailableCommands)
            {
                var (isAvail, flagsReq) = GetRequiredFlag(cmd);

                if (!isAvail)
                {
                    continue;
                }

                if (flagsReq.Length != 0)
                {
                    _commandPermissions.AdminCommands.Add(cmdName, flagsReq);
                }
                else
                {
                    _commandPermissions.AnyCommands.Add(cmdName);
                }
            }

            foreach (var spec in _toolshed.DefaultEnvironment.AllCommands())
            {
                var (isAvail, flagsReq) = GetRequiredFlag(spec.Cmd);

                if (!isAvail)
                {
                    continue;
                }

                if (flagsReq.Length != 0)
                {
                    _toolshedCommandPermissions.AdminCommands.TryAdd(spec.Cmd.Name, flagsReq);
                }
                else
                {
                    _toolshedCommandPermissions.AnyCommands.Add(spec.Cmd.Name);
                }
            }

            // Load flags for engine commands, since those don't have the attributes.
            if (_res.TryContentFileRead(new ResPath("/engineCommandPerms.yml"), out var efs))
            {
                _commandPermissions.LoadPermissionsFromStream(efs);
            }

            if (_res.TryContentFileRead(new ResPath("/toolshedEngineCommandPerms.yml"), out var toolshedPerms))
            {
                _toolshedCommandPermissions.LoadPermissionsFromStream(toolshedPerms);
            }

            _toolshed.ActivePermissionController = this;

            InitializeMetrics();
        }

        public void PromoteHost(ICommonSession player)
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
        private void UpdateAdminStatus(ICommonSession session)
        {
            var msg = new MsgUpdateAdminStatus();

            var commands = new List<string>(_commandPermissions.AnyCommands);

            if (_admins.TryGetValue(session, out var adminData))
            {
                msg.Admin = adminData.Data;

                commands.AddRange(_commandPermissions.AdminCommands
                    .Where(p => p.Value.Any(f => adminData.Data.HasFlag(f)))
                    .Select(p => p.Key));
            }

            msg.AvailableCommands = commands.ToArray();

            _netMgr.ServerSendMessage(msg, session.Channel);
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
                if (_admins.Remove(e.Session, out var reg ) && _cfg.GetCVar(CCVars.AdminAnnounceLogout))
                {
                    if (reg.Data.Stealth)
                    {
                        _chat.SendAdminAnnouncement(Loc.GetString("admin-manager-admin-logout-message",
                            ("name", e.Session.Name)), flagWhitelist: AdminFlags.Stealth);

                    }
                    else
                    {
                        _chat.SendAdminAnnouncement(Loc.GetString("admin-manager-admin-logout-message",
                            ("name", e.Session.Name)));
                    }
                }
            }
        }

        private async void LoginAdminMaybe(ICommonSession session)
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

            if (session.ContentData()!.Stealthed)
                reg.Data.Stealth = true;

            if (!session.ContentData()!.ExplicitlyDeadminned)
            {
                reg.Data.Active = true;

                if (_cfg.GetCVar(CCVars.AdminAnnounceLogin))
                {
                    if (reg.Data.Stealth)
                    {

                        _chat.DispatchServerMessage(session, Loc.GetString("admin-manager-stealthed-message"));
                        _chat.SendAdminAnnouncement(Loc.GetString("admin-manager-admin-login-message",
                            ("name", session.Name)), flagWhitelist: AdminFlags.Stealth);
                    }
                    else
                    {
                        _chat.SendAdminAnnouncement(Loc.GetString("admin-manager-admin-login-message",
                            ("name", session.Name)));
                    }
                }

                SendPermsChangedEvent(session);
            }

            UpdateAdminStatus(session);
        }

        private async Task<(AdminData dat, int? rankId, bool specialLogin)?> LoadAdminData(ICommonSession session)
        {
            var promoteHost = IsLocal(session) && _cfg.GetCVar(CCVars.ConsoleLoginLocal)
                              || _promotedPlayers.Contains(session.UserId)
                              || session.Name == _cfg.GetCVar(CCVars.ConsoleLoginHostUser);

            if (promoteHost)
            {
                var data = new AdminData
                {
                    Title = Loc.GetString("admin-manager-admin-data-host-title"),
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

                if (dbData.Title != null  && _cfg.GetCVar(CCVars.AdminUseCustomNamesAdminRank))
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

        private static bool IsLocal(ICommonSession player)
        {
            var ep = player.Channel.RemoteEndPoint;
            var addr = ep.Address;
            if (addr.IsIPv4MappedToIPv6)
            {
                addr = addr.MapToIPv4();
            }

            return Equals(addr, System.Net.IPAddress.Loopback) || Equals(addr, System.Net.IPAddress.IPv6Loopback);
        }

        public bool TryGetCommandFlags(CommandSpec command, out AdminFlags[]? flags)
        {
            var cmdName = command.Cmd.Name;

            if (_toolshedCommandPermissions.AnyCommands.Contains(cmdName))
            {
                // Anybody can use this command.
                flags = null;
                return true;
            }

            if (_toolshedCommandPermissions.AdminCommands.TryGetValue(cmdName, out flags))
            {
                return true;
            }

            flags = null;
            return false;
        }

        public bool CanCommand(ICommonSession session, string cmdName)
        {
            if (_commandPermissions.AnyCommands.Contains(cmdName))
            {
                // Anybody can use this command.
                return true;
            }

            if (!_commandPermissions.AdminCommands.TryGetValue(cmdName, out var flagsReq))
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

        public bool CheckInvokable(CommandSpec command, ICommonSession? user, out IConError? error)
        {
            if (user is null)
            {
                error = null;
                return true; // Server console.
            }

            var name = command.Cmd.Name;
            if (!TryGetCommandFlags(command, out var flags))
            {
                // Command is missing permissions.
                error = new CommandPermissionsUnassignedError(command);
                return false;
            }

            if (flags is null)
            {
                // Anyone can execute this.
                error = null;
                return true;
            }

            var data = GetAdminData(user);
            if (data == null)
            {
                // Player isn't an admin.
                error = new NoPermissionError(command);
                return false;
            }

            foreach (var flag in flags)
            {
                if (data.HasFlag(flag))
                {
                    error = null;
                    return true;
                }
            }

            error = new NoPermissionError(command);
            return false;
        }

        private static (bool isAvail, AdminFlags[] flagsReq) GetRequiredFlag(object cmd)
        {
            MemberInfo type = cmd.GetType();

            if (cmd is ConsoleHost.RegisteredCommand registered)
            {
                type = registered.Callback.Method;
            }

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

        public bool CanViewVar(ICommonSession session)
        {
            return CanCommand(session, "vv");
        }

        public bool CanAdminPlace(ICommonSession session)
        {
            return GetAdminData(session)?.CanAdminPlace() ?? false;
        }

        public bool CanScript(ICommonSession session)
        {
            return GetAdminData(session)?.CanScript() ?? false;
        }

        public bool CanAdminMenu(ICommonSession session)
        {
            return GetAdminData(session)?.CanAdminMenu() ?? false;
        }

        public bool CanAdminReloadPrototypes(ICommonSession session)
        {
            return GetAdminData(session)?.CanAdminReloadPrototypes() ?? false;
        }

        private void SendPermsChangedEvent(ICommonSession session)
        {
            var flags = GetAdminData(session)?.Flags;
            OnPermsChanged?.Invoke(new AdminPermsChangedEventArgs(session, flags));
        }

        private sealed class AdminReg
        {
            public readonly ICommonSession Session;

            public AdminData Data;
            public int? RankId;

            // Such as console.loginlocal or promotehost
            public bool IsSpecialLogin;

            public AdminReg(ICommonSession session, AdminData data)
            {
                Data = data;
                Session = session;
            }
        }
    }
}

public record struct CommandPermissionsUnassignedError(CommandSpec Command) : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromMarkupOrThrow($"The command {Command.FullName()} is missing permission flags and cannot be executed.");
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}


public record struct NoPermissionError(CommandSpec Command) : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromMarkupOrThrow($"You do not have permission to execute {Command.FullName()}");
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}
