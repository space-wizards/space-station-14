using Content.Server.Cargo;
using Content.Server.Interfaces;
using Content.Server.Interfaces.Chat;
using Content.Server.Interfaces.GameTicking;
using Content.Server.Preferences;
using Content.Server.Sandbox;
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
                "InteractionOutline",
                "MeleeWeaponArcAnimation",
                "AnimationsTest",
                "ItemStatus"
            };

            foreach (var ignoreName in registerIgnore)
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
            IoCManager.Resolve<ISandboxManager>().Initialize();
            IoCManager.Resolve<IServerPreferencesManager>().Initialize();
        }

        public override void Update(ModUpdateLevel level, FrameEventArgs frameEventArgs)
        {
            base.Update(level, frameEventArgs);

            _gameTicker.Update(frameEventArgs);
            switch (level)
            {
                case ModUpdateLevel.PreEngine:
                {
                    IoCManager.Resolve<IGalacticBankManager>().Update(frameEventArgs);
                    break;
                }
            }
        }
    }
}
