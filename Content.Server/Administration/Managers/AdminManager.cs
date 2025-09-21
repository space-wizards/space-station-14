using System.Linq;
using System.Threading.Tasks;
using Content.Server.Chat.Managers;
using Content.Server.Database;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Players;
using Robust.Server.Console;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server.Administration.Managers
{
    public sealed partial class AdminManager : SharedAdminManager, IAdminManager, IConGroupControllerImplementation
    {
        [Dependency] private readonly IServerDbManager _dbManager = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IServerNetManager _netMgr = default!;
        [Dependency] private readonly IChatManager _chat = default!;
        [Dependency] private readonly IConGroupController _conGroup = default!;

        private readonly Dictionary<ICommonSession, AdminReg> _admins = new();
        private readonly HashSet<NetUserId> _promotedPlayers = new();

        public event Action<AdminPermsChangedEventArgs>? OnPermsChanged;

        public IEnumerable<ICommonSession> ActiveAdmins => _admins
            .Where(p => p.Value.Data.Active)
            .Select(p => p.Key);

        public IEnumerable<ICommonSession> AllAdmins => _admins.Select(p => p.Key);

        private ISawmill _sawmill = default!;

        public override void Initialize()
        {
            base.Initialize();
            _netMgr.RegisterNetMessage<MsgUpdateAdminStatus>();
            PlayerMan.PlayerStatusChanged += PlayerStatusChanged;
            InitializeMetrics();
            _conGroup.Implementation = this;
            Toolshed.ActivePermissionController = this;
        }

        public override AdminData? GetAdminData(ICommonSession session, bool includeDeAdmin = false)
        {
            if (_admins.TryGetValue(session, out var reg) && (reg.Data.Active || includeDeAdmin))
            {
                return reg.Data;
            }

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

            UpdateDatabaseDeadminnedState(session, true);
            reg.Data.Active = false;

            SendPermsChangedEvent(session);
            UpdateAdminStatus(session);
        }

        private async void UpdateDatabaseDeadminnedState(ICommonSession player, bool newState)
        {
            try
            {
                // NOTE: This function gets called if you deadmin/readmin from a transient admin status.
                // (e.g. loginlocal)
                // In which case there may not be a database record.
                // The DB function handles this scenario fine, but it's worth noting.
                await _dbManager.UpdateAdminDeadminnedAsync(player.UserId, newState);
            }
            catch (Exception)
            {
                _sawmill.Error("Failed to save deadmin state to database for {Admin}", player.UserId);
            }
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

            UpdateDatabaseDeadminnedState(session, false);
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

                    if (curAdmin.Data.Active)
                    {
                        aData.Active = true;

                        _chat.DispatchServerMessage(player, Loc.GetString("admin-manager-admin-permissions-updated-message"));
                    }
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

        public void PromoteHost(ICommonSession player)
        {
            _promotedPlayers.Add(player.UserId);

            ReloadAdmin(player);
        }

        // NOTE: Also sends commands list for non admins..
        private void UpdateAdminStatus(ICommonSession session)
        {
            var msg = new MsgUpdateAdminStatus();

            var commands = new List<string>(CommandPermissions.AnyCommands);

            if (_admins.TryGetValue(session, out var adminData))
            {
                msg.Admin = adminData.Data;

                commands.AddRange(CommandPermissions.AdminCommands
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

            if (reg.Data.Active)
            {
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
            var result = await LoadAdminDataCore(session);

            // Make sure admin didn't disconnect while data was loading.
            if (session.Status != SessionStatus.InGame)
                return null;

            return result;
        }

        private async Task<(AdminData dat, int? rankId, bool specialLogin)?> LoadAdminDataCore(ICommonSession session)
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
                    Active = true,
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

                if (dbData.Suspended)
                {
                    // Suspended admins don't count.
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
                    Flags = flags,
                    Active = !dbData.Deadminned,
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

        protected override (bool isAvail, AdminFlags[] flagsReq) GetRequiredFlags(object cmd)
        {
            if (cmd is not ConsoleHost.RegisteredCommand registered)
                return base.GetRequiredFlags(cmd);

            var method = registered.Callback.Method;
            if (Attribute.IsDefined(method, typeof(AnyCommandAttribute)))
                return (true, []);

            var attribs = Attribute.GetCustomAttributes(method, typeof(AdminCommandAttribute))
                .Cast<AdminCommandAttribute>()
                .Select(p => p.Flags)
                .ToArray();

            return (attribs.Length != 0, attribs);
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
