using Content.Client;
using Content.Client.Interfaces.Parallax;
using Robust.Shared.ContentPack;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.IoC;
using Robust.UnitTesting;

namespace Content.IntegrationTests
{
    public abstract class ContentIntegrationTest : RobustIntegrationTest
    {
        protected override ClientIntegrationInstance StartClient(ClientIntegrationOptions options = null)
        {
            options ??= new ClientIntegrationOptions();
            // ReSharper disable once RedundantNameQualifier
            options.ClientContentAssembly = typeof(Client.EntryPoint).Assembly;
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
    }
}
