using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Server.Afk;
using Content.Server.AI.Utility;
using Content.Server.AI.Utility.Considerations;
using Content.Server.AI.WorldState;
using Content.Server.Chat.Managers;
using Content.Server.Connection;
using Content.Server.Database;
using Content.Server.DeviceNetwork;
using Content.Server.EUI;
using Content.Server.Holiday;
using Content.Server.Holiday.Interfaces;
using Content.Server.Module;
using Content.Server.MoMMI;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.Objectives;
using Content.Server.Objectives.Interfaces;
using Content.Server.PDA.Managers;
using Content.Server.Preferences.Managers;
using Content.Server.Sandbox;
using Content.Server.Speech;
using Content.Server.Voting.Managers;
using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.Kitchen;
using Content.Shared.Module;
using Robust.Shared.IoC;

namespace Content.Server.IoC
{
    internal static class ServerContentIoC
    {
        public static void Register()
        {
            IoCManager.Register<IChatManager, ChatManager>();
            IoCManager.Register<IMoMMILink, MoMMILink>();
            IoCManager.Register<ISandboxManager, SandboxManager>();
            IoCManager.Register<IModuleManager, ServerModuleManager>();
            IoCManager.Register<IServerPreferencesManager, ServerPreferencesManager>();
            IoCManager.Register<IServerDbManager, ServerDbManager>();
            IoCManager.Register<RecipeManager, RecipeManager>();
            IoCManager.Register<AlertManager, AlertManager>();
            IoCManager.Register<ActionManager, ActionManager>();
            IoCManager.Register<IUplinkManager,UplinkManager>();
            IoCManager.Register<INodeGroupFactory, NodeGroupFactory>();
            IoCManager.Register<BlackboardManager, BlackboardManager>();
            IoCManager.Register<ConsiderationsManager, ConsiderationsManager>();
            IoCManager.Register<IAccentManager, AccentManager>();
            IoCManager.Register<IConnectionManager, ConnectionManager>();
            IoCManager.Register<IObjectivesManager, ObjectivesManager>();
            IoCManager.Register<IAdminManager, AdminManager>();
            IoCManager.Register<IDeviceNetwork, DeviceNetwork.DeviceNetwork>();
            IoCManager.Register<EuiManager, EuiManager>();
            IoCManager.Register<IVoteManager, VoteManager>();
            IoCManager.Register<INpcBehaviorManager, NpcBehaviorManager>();
            IoCManager.Register<IPlayerLocator, PlayerLocator>();
            IoCManager.Register<IAfkManager, AfkManager>();
        }
    }
}
