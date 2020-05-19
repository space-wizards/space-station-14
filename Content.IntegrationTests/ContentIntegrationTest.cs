using Content.Client;
using Content.Client.Interfaces.Parallax;
using Content.Server;
using Content.Server.GameTicking;
using Content.Server.Interfaces.GameTicking;
using Robust.Shared.ContentPack;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.IoC;
using Robust.UnitTesting;
using EntryPoint = Content.Client.EntryPoint;

namespace Content.IntegrationTests
{
    public abstract class ContentIntegrationTest : RobustIntegrationTest
    {
        protected override ClientIntegrationInstance StartClient(ClientIntegrationOptions options = null)
        {
            options ??= new ClientIntegrationOptions();
            // ReSharper disable once RedundantNameQualifier
            options.ClientContentAssembly = typeof(EntryPoint).Assembly;
            options.SharedContentAssembly = typeof(Shared.EntryPoint).Assembly;
            options.BeforeStart += () =>
            {
                // Connecting to Discord is a massive waste of time.
                // Basically just makes the CI logs a mess.
                IoCManager.Resolve<IConfigurationManager>().SetCVar("discord.enabled", false);
                IoCManager.Resolve<IModLoader>().SetModuleBaseCallbacks(new ClientModuleTestingCallbacks
                {
                    ClientBeforeIoC = () =>
                    {
                        IoCManager.Register<IParallaxManager, DummyParallaxManager>(true);
                    }
                });
            };
            return base.StartClient(options);
        }

        protected override ServerIntegrationInstance StartServer(ServerIntegrationOptions options = null)
        {
            options ??= new ServerIntegrationOptions();
            options.ServerContentAssembly = typeof(Server.EntryPoint).Assembly;
            options.SharedContentAssembly = typeof(Shared.EntryPoint).Assembly;
            return base.StartServer(options);
        }

        protected ServerIntegrationInstance StartServerDummyTicker(ServerIntegrationOptions options = null)
        {
            options ??= new ServerIntegrationOptions();
            options.BeforeStart += () =>
            {
                IoCManager.Resolve<IModLoader>().SetModuleBaseCallbacks(new ServerModuleTestingCallbacks
                {
                    ServerBeforeIoC = () =>
                    {
                        IoCManager.Register<IGameTicker, DummyGameTicker>(true);
                    }
                });
            };

            return StartServer(options);
        }
    }
}
