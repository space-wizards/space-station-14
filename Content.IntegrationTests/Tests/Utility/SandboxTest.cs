using Content.Client.IoC;
using Content.Client.Parallax.Managers;
using Robust.Client;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.UnitTesting;

namespace Content.IntegrationTests.Tests.Utility;

public sealed class SandboxTest
{
    [Test]
    public async Task Test()
    {
        // Not using PoolManager.GetServerClient() because we want to avoid having to unnecessarily create & destroy a
        // server. This all becomes unnecessary if ever the test becomes non-destructive or the no-server option
        // actually creates a pair without a server.

        var logHandler = new PoolTestLogHandler("CLIENT");
        logHandler.ActivateContext(TestContext.Out);
        var options = new RobustIntegrationTest.ClientIntegrationOptions
        {
            ContentStart = true,
            OverrideLogHandler = () => logHandler,
            ContentAssemblies = new[]
            {
                typeof(Shared.Entry.EntryPoint).Assembly,
                typeof(Client.Entry.EntryPoint).Assembly
            },
            Options = new GameControllerOptions { LoadConfigAndUserData = false }
        };

        options.BeforeStart += () =>
        {
            IoCManager.Resolve<IModLoader>().SetModuleBaseCallbacks(new ClientModuleTestingCallbacks
            {
                ClientBeforeIoC = () =>
                {
                    IoCManager.Register<IParallaxManager, DummyParallaxManager>(true);
                    IoCManager.Resolve<ILogManager>().GetSawmill("loc").Level = LogLevel.Error;
                    IoCManager.Resolve<IConfigurationManager>()
                        .OnValueChanged(RTCVars.FailureLogLevel, value => logHandler.FailureLevel = value, true);
                }
            });
        };

        using var client = new RobustIntegrationTest.ClientIntegrationInstance(options);
        await client.WaitIdleAsync();
        await client.CheckSandboxed(typeof(Client.Entry.EntryPoint).Assembly);
        await client.CheckSandboxed(typeof(Shared.IoC.SharedContentIoC).Assembly);
    }
}
