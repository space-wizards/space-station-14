using Robust.Shared.Configuration;
using Robust.Shared.Log;
using Robust.UnitTesting;

namespace Content.IntegrationTests.Tests;

public sealed class LogErrorTest
{
    /// <summary>
    ///     This test ensures that error logs cause tests to fail.
    /// </summary>
    [Test]
    public async Task TestLogErrorCausesTestFailure()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
        var server = pair.Server;
        var client = pair.Client;

        var cfg = server.ResolveDependency<IConfigurationManager>();
        var logmill = server.ResolveDependency<ILogManager>().RootSawmill;

        // Default cvar is properly configured
        Assert.That(cfg.GetCVar(RTCVars.FailureLogLevel), Is.EqualTo(LogLevel.Error));

        // Warnings don't cause tests to fail.
        await server.WaitPost(() => logmill.Warning("test"));

        // But errors do
        await server.WaitPost(() => Assert.Throws<AssertionException>(() => logmill.Error("test")));
        await client.WaitPost(() => Assert.Throws<AssertionException>(() => logmill.Error("test")));

        await pair.CleanReturnAsync();
    }
}
