using Content.Server.AI.Utility.Considerations;
using Content.Server.AI.WorldState;
using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;
using Content.Server.Interfaces;
using Content.Server.Interfaces.Chat;
using Content.Server.Body.Network;
using Content.Server.Interfaces.GameTicking;
using Content.Server.Interfaces.PDA;
using Content.Server.Sandbox;
using Content.Shared.Kitchen;
using Robust.Server.Interfaces.Player;
using Robust.Shared.ContentPack;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Log;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Timing;
using Content.Server.GameObjects.Components.Mobs.Speech;

namespace Content.Server
{
    public class EntryPoint : GameServer
    {
        private IGameTicker _gameTicker;
        private StatusShell _statusShell;

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

            if (TestingCallbacks != null)
            {
                var cast = (ServerModuleTestingCallbacks) TestingCallbacks;
                cast.ServerBeforeIoC?.Invoke();
            }

            IoCManager.BuildGraph();

            IoCManager.Resolve<IBodyNetworkFactory>().DoAutoRegistrations();

            _gameTicker = IoCManager.Resolve<IGameTicker>();

            IoCManager.Resolve<IServerNotifyManager>().Initialize();
            IoCManager.Resolve<IChatManager>().Initialize();

            var playerManager = IoCManager.Resolve<IPlayerManager>();

            _statusShell = new StatusShell();

            var logManager = IoCManager.Resolve<ILogManager>();
            logManager.GetSawmill("Storage").Level = LogLevel.Info;

            IoCManager.Resolve<IServerPreferencesManager>().StartInit();
            IoCManager.Resolve<INodeGroupFactory>().Initialize();
            IoCManager.Resolve<ISandboxManager>().Initialize();
            IoCManager.Resolve<IAccentManager>().Initialize();
        }

        public override void PostInit()
        {
            base.PostInit();

            IoCManager.Resolve<IServerPreferencesManager>().FinishInit();
            _gameTicker.Initialize();
            IoCManager.Resolve<RecipeManager>().Initialize();
            IoCManager.Resolve<BlackboardManager>().Initialize();
            IoCManager.Resolve<ConsiderationsManager>().Initialize();
            IoCManager.Resolve<IPDAUplinkManager>().Initialize();
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
            }
        }
    }
}
