using Content.Server.Administration;
using Content.Server.AI.Utility;
using Content.Server.AI.Utility.Considerations;
using Content.Server.AI.WorldState;
using Content.Server.Database;
using Content.Server.Eui;
using Content.Server.GameObjects.Components.Mobs.Speech;
using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;
using Content.Server.Holiday.Interfaces;
using Content.Server.Interfaces;
using Content.Server.Interfaces.Chat;
using Content.Server.Interfaces.GameTicking;
using Content.Server.Interfaces.PDA;
using Content.Server.Sandbox;
using Content.Server.Voting;
using Content.Shared.Actions;
using Content.Shared.Kitchen;
using Content.Shared.Alert;
using Robust.Server.Player;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Timing;

namespace Content.Server
{
    public class EntryPoint : GameServer
    {
        private IGameTicker _gameTicker;
        private EuiManager _euiManager;
        private StatusShell _statusShell;
        private IVoteManager _voteManager;

        /// <inheritdoc />
        public override void Init()
        {
            base.Init();

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

            _gameTicker = IoCManager.Resolve<IGameTicker>();
            _euiManager = IoCManager.Resolve<EuiManager>();
            _voteManager = IoCManager.Resolve<IVoteManager>();

            IoCManager.Resolve<IServerNotifyManager>().Initialize();
            IoCManager.Resolve<IChatManager>().Initialize();

            var playerManager = IoCManager.Resolve<IPlayerManager>();

            _statusShell = new StatusShell();

            var logManager = IoCManager.Resolve<ILogManager>();
            logManager.GetSawmill("Storage").Level = LogLevel.Info;
            logManager.GetSawmill("db.ef").Level = LogLevel.Info;

            IoCManager.Resolve<IConnectionManager>().Initialize();
            IoCManager.Resolve<IServerDbManager>().Init();
            IoCManager.Resolve<IServerPreferencesManager>().Init();
            IoCManager.Resolve<INodeGroupFactory>().Initialize();
            IoCManager.Resolve<ISandboxManager>().Initialize();
            IoCManager.Resolve<IAccentManager>().Initialize();
            _voteManager.Initialize();
        }

        public override void PostInit()
        {
            base.PostInit();

            IoCManager.Resolve<IHolidayManager>().Initialize();
            _gameTicker.Initialize();
            IoCManager.Resolve<RecipeManager>().Initialize();
            IoCManager.Resolve<AlertManager>().Initialize();
            IoCManager.Resolve<ActionManager>().Initialize();
            IoCManager.Resolve<BlackboardManager>().Initialize();
            IoCManager.Resolve<ConsiderationsManager>().Initialize();
            IoCManager.Resolve<IPDAUplinkManager>().Initialize();
            IoCManager.Resolve<IAdminManager>().Initialize();
            IoCManager.Resolve<INpcBehaviorManager>().Initialize();
            _euiManager.Initialize();
        }

        public override void Update(ModUpdateLevel level, FrameEventArgs frameEventArgs)
        {
            base.Update(level, frameEventArgs);

            switch (level)
            {
                case ModUpdateLevel.PreEngine:
                {
                    _gameTicker.Update(frameEventArgs);
                    break;
                }
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
