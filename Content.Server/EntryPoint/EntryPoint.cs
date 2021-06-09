using Content.Server.Administration.Managers;
using Content.Server.AI.Utility;
using Content.Server.AI.Utility.Considerations;
using Content.Server.AI.WorldState;
using Content.Server.Chat.Managers;
using Content.Server.Connection;
using Content.Server.Database;
using Content.Server.EUI;
using Content.Server.GameTicking;
using Content.Server.Holiday.Interfaces;
using Content.Server.IoC;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.Notification.Managers;
using Content.Server.PDA.Managers;
using Content.Server.Preferences.Managers;
using Content.Server.Sandbox;
using Content.Server.Shell;
using Content.Server.Speech;
using Content.Server.Voting.Managers;
using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.Kitchen;
using Robust.Server.Player;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Timing;

namespace Content.Server.EntryPoint
{
    public class EntryPoint : GameServer
    {
        private IGameTicker _gameTicker = default!;
        private EuiManager _euiManager = default!;
        private StatusShell _statusShell = default!;
        private IVoteManager _voteManager = default!;

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
