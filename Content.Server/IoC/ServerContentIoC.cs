using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Administration.Notes;
using Content.Server.Afk;
using Content.Server.Chat.Managers;
using Content.Server.Connection;
using Content.Server.Database;
using Content.Server.Discord;
using Content.Server.EUI;
using Content.Server.GhostKick;
using Content.Server.Info;
using Content.Server.Maps;
using Content.Server.MoMMI;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.Players.PlayTimeTracking;
using Content.Server.Preferences.Managers;
using Content.Server.ServerInfo;
using Content.Server.ServerUpdates;
using Content.Server.Voting.Managers;
using Content.Server.Worldgen.Tools;
using Content.Shared.Administration.Logs;
using Content.Shared.Administration.Managers;
using Content.Shared.Kitchen;
using Content.Shared.Players.PlayTimeTracking;

namespace Content.Server.IoC
{
    internal static class ServerContentIoC
    {
        public static void Register()
        {
            var collection = IoCManager.Instance!;

            collection.Register<IChatManager, ChatManager>();
            collection.Register<IChatSanitizationManager, ChatSanitizationManager>();
            collection.Register<IMoMMILink, MoMMILink>();
            collection.Register<IServerPreferencesManager, ServerPreferencesManager>();
            collection.Register<IServerDbManager, ServerDbManager>();
            collection.Register<RecipeManager, RecipeManager>();
            collection.Register<INodeGroupFactory, NodeGroupFactory>();
            collection.Register<IConnectionManager, ConnectionManager>();
            collection.Register<ServerUpdateManager>();
            collection.Register<IAdminManager, AdminManager>();
            collection.Register<ISharedAdminManager, AdminManager>();
            collection.Register<EuiManager, EuiManager>();
            collection.Register<IVoteManager, VoteManager>();
            collection.Register<IPlayerLocator, PlayerLocator>();
            collection.Register<IAfkManager, AfkManager>();
            collection.Register<IGameMapManager, GameMapManager>();
            collection.Register<RulesManager, RulesManager>();
            collection.Register<IBanManager, BanManager>();
            collection.Register<ContentNetworkResourceManager>();
            collection.Register<IAdminNotesManager, AdminNotesManager>();
            collection.Register<GhostKickManager>();
            collection.Register<ISharedAdminLogManager, AdminLogManager>();
            collection.Register<IAdminLogManager, AdminLogManager>();
            collection.Register<PlayTimeTrackingManager>();
            collection.Register<UserDbDataManager>();
            collection.Register<ServerInfoManager>();
            collection.Register<PoissonDiskSampler>();
            collection.Register<DiscordWebhook>();
            collection.Register<ServerDbEntryManager>();
            collection.Register<ISharedPlaytimeManager, PlayTimeTrackingManager>();
        }
    }
}
