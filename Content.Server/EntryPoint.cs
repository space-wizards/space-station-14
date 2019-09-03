using Content.Server.Chat;
using Content.Server.GameTicking;
using Content.Server.Interfaces;
using Content.Server.Interfaces.Chat;
using Content.Server.Interfaces.GameTicking;
using Content.Shared.Interfaces;
using Robust.Server.Interfaces.Player;
using Robust.Shared.ContentPack;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Log;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Timing;

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

            var registerIgnore = new[]
            {
                "ConstructionGhost",
                "IconSmooth",
                "SubFloorHide",
                "LowWall",
                "Window",
                "CharacterInfo",
            };

            foreach (var ignoreName in registerIgnore)
            {
                factory.RegisterIgnore(ignoreName);
            }

            IoCManager.Register<ISharedNotifyManager, ServerNotifyManager>();
            IoCManager.Register<IServerNotifyManager, ServerNotifyManager>();
            IoCManager.Register<IGameTicker, GameTicker>();
            IoCManager.Register<IChatManager, ChatManager>();
            IoCManager.Register<IMoMMILink, MoMMILink>();
            if (TestingCallbacks != null)
            {
                var cast = (ServerModuleTestingCallbacks) TestingCallbacks;
                cast.ServerBeforeIoC?.Invoke();
            }
            IoCManager.BuildGraph();

            _gameTicker = IoCManager.Resolve<IGameTicker>();

            IoCManager.Resolve<IServerNotifyManager>().Initialize();
            IoCManager.Resolve<IChatManager>().Initialize();

            var playerManager = IoCManager.Resolve<IPlayerManager>();

            _statusShell = new StatusShell();

            var logManager = IoCManager.Resolve<ILogManager>();
            logManager.GetSawmill("Storage").Level = LogLevel.Info;
        }

        public override void PostInit()
        {
            base.PostInit();

            _gameTicker.Initialize();
        }

        public override void Update(ModUpdateLevel level, FrameEventArgs frameEventArgs)
        {
            base.Update(level, frameEventArgs);

            _gameTicker.Update(frameEventArgs);
        }
    }
}
