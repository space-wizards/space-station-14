using System.Threading.Tasks;
using NUnit.Framework;

namespace Content.IntegrationTests.Tests.Utility;

public sealed class SandboxTest
{
    [Test]
    public async Task Test()
    {
        await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoServer = true, Destructive = true});
        var client = pairTracker.Pair.Client;
        await client.CheckSandboxed(typeof(Client.Entry.EntryPoint).Assembly);
        await pairTracker.CleanReturnAsync();
    }
}
