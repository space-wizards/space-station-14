using System.Linq;
using System.Threading.Tasks;
using Content.Server.Administration.Managers;
using Content.Server.Database;
using Content.Server.EUI;
using Content.Shared.Administration;
using Content.Shared.Eui;
using Robust.Server.Player;
using Robust.Shared.Network;
using DbAdminRank = Content.Server.Database.AdminRank;
using static Content.Shared.Administration.PermissionsEuiMsg;


namespace Content.Server.Administration.UI
{
    public sealed class PermissionsEui : BaseEui
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IServerDbManager _db = default!;
        [Dependency] private readonly IAdminManager _adminManager = default!;

        private bool _isLoading;

        private readonly List<(Admin a, string? lastUserName)> _admins = new List<(Admin, string? lastUserName)>();
        private readonly List<DbAdminRank> _adminRanks = new();

        public PermissionsEui()
        {
            IoCManager.InjectDependencies(this);
        }

        public override void Opened()
        {
            base.Opened();

            StateDirty();
            LoadFromDb();
            _adminManager.OnPermsChanged += AdminManagerOnPermsChanged;
        }

        public override void Closed()
        {
            base.Closed();

            _adminManager.OnPermsChanged -= AdminManagerOnPermsChanged;
        }

        private void AdminManagerOnPermsChanged(AdminPermsChangedEventArgs obj)
        {
            // Close UI if user loses +PERMISSIONS.
            if (obj.Player == Player && !UserAdminFlagCheck(AdminFlags.Permissions))
            {
                Close();
            }
        }

        public override EuiStateBase GetNewState()
        {
            if (_isLoading)
            {
                return new PermissionsEuiState
                {
                    IsLoading = true
                };
            }

            return new PermissionsEuiState
            {
                Admins = _admins.Select(p => new PermissionsEuiState.AdminData
                {
                    PosFlags = AdminFlagsHelper.NamesToFlags(p.a.Flags.Where(f => !f.Negative).Select(f => f.Flag)),
                    NegFlags = AdminFlagsHelper.NamesToFlags(p.a.Flags.Where(f => f.Negative).Select(f => f.Flag)),
                    Title = p.a.Title,
                    RankId = p.a.AdminRankId,
                    UserId = new NetUserId(p.a.UserId),
                    UserName = p.lastUserName
                }).ToArray(),

                AdminRanks = _adminRanks.ToDictionary(a => a.Id, a => new PermissionsEuiState.AdminRankData
                {
                    Flags = AdminFlagsHelper.NamesToFlags(a.Flags.Select(p => p.Flag)),
                    Name = a.Name
                })
            };
        }

        public override async void HandleMessage(EuiMessageBase msg)
        {
            base.HandleMessage(msg);

            switch (msg)
            {
                case AddAdmin ca:
                {
                    await HandleCreateAdmin(ca);
                    break;
                }

                case UpdateAdmin ua:
                {
                    await HandleUpdateAdmin(ua);
                    break;
                }

                case RemoveAdmin ra:
                {
                    await HandleRemoveAdmin(ra);
                    break;
                }

                case AddAdminRank ar:
                {
                    await HandleAddAdminRank(ar);
                    break;
                }

                case UpdateAdminRank ur:
                {
                    await HandleUpdateAdminRank(ur);
                    break;
                }

                case RemoveAdminRank ra:
                {
                    await HandleRemoveAdminRank(ra);
                    break;
                }
            }

            if (!IsShutDown)
            {
                LoadFromDb();
            }
        }

        private async Task HandleRemoveAdminRank(RemoveAdminRank rr)
        {
            var rank = await _db.GetAdminRankAsync(rr.Id);
            if (rank == null)
            {
                return;
            }

            if (!CanTouchRank(rank))
            {
                Logger.WarningS("admin.perms", $"{Player} tried to remove higher-ranked admin rank {rank.Name}");
                return;
            }

            await _db.RemoveAdminRankAsync(rr.Id);

            _adminManager.ReloadAdminsWithRank(rr.Id);
        }

        private async Task HandleUpdateAdminRank(UpdateAdminRank ur)
        {
            var rank = await _db.GetAdminRankAsync(ur.Id);
            if (rank == null)
            {
                return;
            }

            if (!CanTouchRank(rank))
            {
                Logger.WarningS("admin.perms", $"{Player} tried to update higher-ranked admin rank {rank.Name}");
                return;
            }

            if (!UserAdminFlagCheck(ur.Flags))
            {
                Logger.WarningS("admin.perms", $"{Player} tried to give a rank permissions above their authorization.");
                return;
            }

            rank.Flags = GenRankFlagList(ur.Flags);
            rank.Name = ur.Name;

            await _db.UpdateAdminRankAsync(rank);

            var flagText = string.Join(' ', AdminFlagsHelper.FlagsToNames(ur.Flags).Select(f => $"+{f}"));
            Logger.InfoS("admin.perms", $"{Player} updated admin rank {rank.Name}/{flagText}.");

            _adminManager.ReloadAdminsWithRank(ur.Id);
        }

        private async Task HandleAddAdminRank(AddAdminRank ar)
        {
            if (!UserAdminFlagCheck(ar.Flags))
            {
                Logger.WarningS("admin.perms", $"{Player} tried to give a rank permissions above their authorization.");
                return;
            }

            var rank = new DbAdminRank
            {
                Name = ar.Name,
                Flags = GenRankFlagList(ar.Flags)
            };

            await _db.AddAdminRankAsync(rank);

            var flagText = string.Join(' ', AdminFlagsHelper.FlagsToNames(ar.Flags).Select(f => $"+{f}"));
            Logger.InfoS("admin.perms", $"{Player} added admin rank {rank.Name}/{flagText}.");
        }

        private async Task HandleRemoveAdmin(RemoveAdmin ra)
        {
            var admin = await _db.GetAdminDataForAsync(ra.UserId);
            if (admin == null)
            {
                // Doesn't exist.
                return;
            }

            if (!CanTouchAdmin(admin))
            {
                Logger.WarningS("admin.perms", $"{Player} tried to remove higher-ranked admin {ra.UserId.ToString()}");
                return;
            }

            await _db.RemoveAdminAsync(ra.UserId);

            var record = await _db.GetPlayerRecordByUserId(ra.UserId);
            Logger.InfoS("admin.perms", $"{Player} removed admin {record?.LastSeenUserName ?? ra.UserId.ToString()}");

            if (_playerManager.TryGetSessionById(ra.UserId, out var player))
            {
                _adminManager.ReloadAdmin(player);
            }
        }

        private async Task HandleUpdateAdmin(UpdateAdmin ua)
        {
            if (!CheckCreatePerms(ua.PosFlags, ua.NegFlags))
            {
                return;
            }

            var admin = await _db.GetAdminDataForAsync(ua.UserId);
            if (admin == null)
            {
                // Was removed in the mean time I guess?
                return;
            }

            if (!CanTouchAdmin(admin))
            {
                Logger.WarningS("admin.perms", $"{Player} tried to modify higher-ranked admin {ua.UserId.ToString()}");
                return;
            }

            admin.Title = ua.Title;
            admin.AdminRankId = ua.RankId;
            admin.Flags = GenAdminFlagList(ua.PosFlags, ua.NegFlags);

            await _db.UpdateAdminAsync(admin);

            var playerRecord = await _db.GetPlayerRecordByUserId(ua.UserId);
            var (bad, rankName) = await FetchAndCheckRank(ua.RankId);
            if (bad)
            {
                return;
            }

            var name = playerRecord?.LastSeenUserName ?? ua.UserId.ToString();
            var title = ua.Title ?? "<no title>";
            var flags = AdminFlagsHelper.PosNegFlagsText(ua.PosFlags, ua.NegFlags);

            Logger.InfoS("admin.perms", $"{Player} updated admin {name} to {title}/{rankName}/{flags}");

            if (_playerManager.TryGetSessionById(ua.UserId, out var player))
            {
                _adminManager.ReloadAdmin(player);
            }
        }

        private async Task HandleCreateAdmin(AddAdmin ca)
        {
            if (!CheckCreatePerms(ca.PosFlags, ca.NegFlags))
            {
                return;
            }

            string name;
            NetUserId userId;
            if (Guid.TryParse(ca.UserNameOrId, out var guid))
            {
                userId = new NetUserId(guid);
                var playerRecord = await _db.GetPlayerRecordByUserId(userId);
                if (playerRecord == null)
                {
                    name = userId.ToString();
                }
                else
                {
                    name = playerRecord.LastSeenUserName;
                }
            }
            else
            {
                // Username entered, resolve user ID from DB.
                var dbPlayer = await _db.GetPlayerRecordByUserName(ca.UserNameOrId);
                if (dbPlayer == null)
                {
                    // username not in DB.
                    // TODO: Notify user.
                    Logger.WarningS("admin.perms",
                        $"{Player} tried to add admin with unknown username {ca.UserNameOrId}.");
                    return;
                }

                userId = dbPlayer.UserId;
                name = ca.UserNameOrId;
            }

            var existing = await _db.GetAdminDataForAsync(userId);
            if (existing != null)
            {
                // Already exists.
                return;
            }

            var (bad, rankName) = await FetchAndCheckRank(ca.RankId);
            if (bad)
            {
                return;
            }

            rankName ??= "<no rank>";

            var admin = new Admin
            {
                Flags = GenAdminFlagList(ca.PosFlags, ca.NegFlags),
                AdminRankId = ca.RankId,
                UserId = userId.UserId,
                Title = ca.Title
            };

            await _db.AddAdminAsync(admin);

            var title = ca.Title ?? "<no title>";
            var flags = AdminFlagsHelper.PosNegFlagsText(ca.PosFlags, ca.NegFlags);

            Logger.InfoS("admin.perms", $"{Player} added admin {name} as {title}/{rankName}/{flags}");

            if (_playerManager.TryGetSessionById(userId, out var player))
            {
                _adminManager.ReloadAdmin(player);
            }
        }

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        private bool CheckCreatePerms(AdminFlags posFlags, AdminFlags negFlags)
        {
            if ((posFlags & negFlags) != 0)
            {
                // Can't have overlapping pos and neg flags.
                // Just deny the entire message.
                return false;
            }

            if (!UserAdminFlagCheck(posFlags))
            {
                // Can't create an admin with higher perms than yourself, obviously.
                Logger.WarningS("admin.perms", $"{Player} tried to grant admin powers above their authorization.");
                return false;
            }

            return true;
        }

        private async Task<(bool bad, string?)> FetchAndCheckRank(int? rankId)
        {
            string? ret = null;
            if (rankId is { } r)
            {
                var rank = await _db.GetAdminRankAsync(r);
                if (rank == null)
                {
                    // Tried to set to nonexistent rank.
                    Logger.WarningS("admin.perms", $"{Player} tried to assign nonexistent admin rank.");
                    return (true, null);
                }

                ret = rank.Name;

                var rankFlags = AdminFlagsHelper.NamesToFlags(rank.Flags.Select(p => p.Flag));
                if (!UserAdminFlagCheck(rankFlags))
                {
                    // Can't assign a rank with flags you don't have yourself.
                    Logger.WarningS("admin.perms", $"{Player} tried to assign admin rank above their authorization.");
                    return (true, null);
                }
            }

            return (false, ret);
        }

        private async void LoadFromDb()
        {
            StateDirty();
            _isLoading = true;
            var (admins, ranks) = await _db.GetAllAdminAndRanksAsync();

            _admins.Clear();
            _admins.AddRange(admins);
            _adminRanks.Clear();
            _adminRanks.AddRange(ranks);

            _isLoading = false;
            StateDirty();
        }

        private static List<AdminFlag> GenAdminFlagList(AdminFlags posFlags, AdminFlags negFlags)
        {
            var posFlagList = AdminFlagsHelper.FlagsToNames(posFlags);
            var negFlagList = AdminFlagsHelper.FlagsToNames(negFlags);

            return posFlagList
                .Select(f => new AdminFlag {Negative = false, Flag = f})
                .Concat(negFlagList.Select(f => new AdminFlag {Negative = true, Flag = f}))
                .ToList();
        }

        private static List<AdminRankFlag> GenRankFlagList(AdminFlags flags)
        {
            return AdminFlagsHelper.FlagsToNames(flags).Select(f => new AdminRankFlag {Flag = f}).ToList();
        }

        private bool UserAdminFlagCheck(AdminFlags flags)
        {
            return _adminManager.HasAdminFlag(Player, flags);
        }

        private bool CanTouchAdmin(Admin admin)
        {
            var posFlags = AdminFlagsHelper.NamesToFlags(admin.Flags.Where(f => !f.Negative).Select(f => f.Flag));
            var rankFlags = AdminFlagsHelper.NamesToFlags(
                admin.AdminRank?.Flags.Select(f => f.Flag) ?? Array.Empty<string>());

            var totalFlags = posFlags | rankFlags;
            return UserAdminFlagCheck(totalFlags);
        }

        private bool CanTouchRank(DbAdminRank rank)
        {
            var rankFlags = AdminFlagsHelper.NamesToFlags(rank.Flags.Select(f => f.Flag));

            return UserAdminFlagCheck(rankFlags);
        }
    }
}
