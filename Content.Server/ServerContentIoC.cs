using Content.Server.Administration;
using Content.Server.AI.Utility.Considerations;
using Content.Server.AI.WorldState;
using Content.Server.Cargo;
using Content.Server.Chat;
using Content.Server.Database;
using Content.Server.Eui;
using Content.Server.GameObjects.Components.Mobs.Speech;
using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;
using Content.Server.GameObjects.Components.Power.PowerNetComponents;
using Content.Server.GameObjects.EntitySystems.DeviceNetwork;
using Content.Server.GameTicking;
using Content.Server.Interfaces;
using Content.Server.Interfaces.Chat;
using Content.Server.Interfaces.GameTicking;
using Content.Server.Interfaces.PDA;
using Content.Server.Objectives;
using Content.Server.Objectives.Interfaces;
using Content.Server.PDA;
using Content.Server.Preferences;
using Content.Server.Sandbox;
using Content.Server.Utility;
using Content.Shared.Actions;
using Content.Shared.Interfaces;
using Content.Shared.Kitchen;
using Content.Shared.Alert;
using Robust.Shared.IoC;

namespace Content.Server
{
    internal static class ServerContentIoC
    {
        public static void Register()
        {
            IoCManager.Register<ISharedNotifyManager, ServerNotifyManager>();
            IoCManager.Register<IServerNotifyManager, ServerNotifyManager>();
            IoCManager.Register<IGameTicker, GameTicker>();
            IoCManager.Register<IChatManager, ChatManager>();
            IoCManager.Register<IMoMMILink, MoMMILink>();
            IoCManager.Register<ISandboxManager, SandboxManager>();
            IoCManager.Register<IModuleManager, ServerModuleManager>();
            IoCManager.Register<IServerPreferencesManager, ServerPreferencesManager>();
            IoCManager.Register<IServerDbManager, ServerDbManager>();
            IoCManager.Register<RecipeManager, RecipeManager>();
            IoCManager.Register<AlertManager, AlertManager>();
            IoCManager.Register<ActionManager, ActionManager>();
            IoCManager.Register<IPDAUplinkManager,PDAUplinkManager>();
            IoCManager.Register<INodeGroupFactory, NodeGroupFactory>();
            IoCManager.Register<INodeGroupManager, NodeGroupManager>();
            IoCManager.Register<IPowerNetManager, PowerNetManager>();
            IoCManager.Register<BlackboardManager, BlackboardManager>();
            IoCManager.Register<ConsiderationsManager, ConsiderationsManager>();
            IoCManager.Register<IAccentManager, AccentManager>();
            IoCManager.Register<IConnectionManager, ConnectionManager>();
            IoCManager.Register<IObjectivesManager, ObjectivesManager>();
            IoCManager.Register<IAdminManager, AdminManager>();
            IoCManager.Register<IDeviceNetwork, DeviceNetwork>();
            IoCManager.Register<EuiManager, EuiManager>();
        }
    }
}
