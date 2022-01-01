using Content.Server.Administration.Managers;
using Content.Server.Afk;
using Content.Server.AI.Utility;
using Content.Server.AI.Utility.Considerations;
using Content.Server.AI.WorldState;
using Content.Server.Chat.Managers;
using Content.Server.Connection;
using Content.Server.Database;
using Content.Server.EUI;
using Content.Server.GameTicking;
using Content.Server.Info;
using Content.Server.IoC;
using Content.Server.Maps;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.Preferences.Managers;
using Content.Server.Sandbox;
using Content.Server.Voting.Managers;
using Content.Shared.Actions;
using Content.Shared.Administration;
using Content.Shared.Alert;
using Content.Shared.Kitchen;
using Robust.Server.Bql;
using Robust.Server.Player;
using Robust.Server.ServerStatus;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Timing;

namespace Content.Server.Entry
{
    public class EntryPoint : GameServer
    {
        private EuiManager _euiManager = default!;
        private IVoteManager _voteManager = default!;

        /// <inheritdoc />
        public override void Init()
        {
            base.Init();

            IoCManager.Resolve<IStatusHost>().SetAczInfo("Content.Client",
                new[] { "Content.Client", "Content.Shared", "Content.Shared.Database" });

            var factory = IoCManager.Resolve<IComponentFactory>();

            factory.DoAutoRegistrations();

            foreach (var ignoreName in IgnoredComponents.List)
            {
                factory.RegisterIgnore(ignoreName);
            }

            ServerContentIoC.Register();

            foreach (var callback in TestingCallbacks)
            {
                var cast = (ServerModuleTestingCallbacks) callback;
                cast.ServerBeforeIoC?.Invoke();
            }

            IoCManager.BuildGraph();
            factory.GenerateNetIds();

            _euiManager = IoCManager.Resolve<EuiManager>();
            _voteManager = IoCManager.Resolve<IVoteManager>();

            IoCManager.Resolve<IChatSanitizationManager>().Initialize();
            IoCManager.Resolve<IChatManager>().Initialize();

            var playerManager = IoCManager.Resolve<IPlayerManager>();

            var logManager = IoCManager.Resolve<ILogManager>();
            logManager.GetSawmill("Storage").Level = LogLevel.Info;
            logManager.GetSawmill("db.ef").Level = LogLevel.Info;

            IoCManager.Resolve<IConnectionManager>().Initialize();
            IoCManager.Resolve<IServerDbManager>().Init();
            IoCManager.Resolve<IServerPreferencesManager>().Init();
            IoCManager.Resolve<INodeGroupFactory>().Initialize();
            IoCManager.Resolve<IGamePrototypeLoadManager>().Initialize();
            _voteManager.Initialize();
        }

        public override void PostInit()
        {
            base.PostInit();

            IoCManager.Resolve<ISandboxManager>().Initialize();
            IoCManager.Resolve<RecipeManager>().Initialize();
            IoCManager.Resolve<AlertManager>().Initialize();
            IoCManager.Resolve<ActionManager>().Initialize();
            IoCManager.Resolve<BlackboardManager>().Initialize();
            IoCManager.Resolve<ConsiderationsManager>().Initialize();
            IoCManager.Resolve<IAdminManager>().Initialize();
            IoCManager.Resolve<INpcBehaviorManager>().Initialize();
            IoCManager.Resolve<IAfkManager>().Initialize();
            IoCManager.Resolve<RulesManager>().Initialize();
            _euiManager.Initialize();

            IoCManager.Resolve<IGameMapManager>().Initialize();
            IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<GameTicker>().PostInitialize();
            IoCManager.Resolve<IBqlQueryManager>().DoAutoRegistrations();
        }

        public override void Update(ModUpdateLevel level, FrameEventArgs frameEventArgs)
        {
            base.Update(level, frameEventArgs);

            switch (level)
            {
                case ModUpdateLevel.PostEngine:
                {
                    _euiManager.SendUpdates();
                    _voteManager.Update();
                    break;
                }
            }
        }
    }
}
