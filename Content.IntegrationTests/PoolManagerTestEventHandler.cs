using NUnit.Framework;

[assembly: Parallelizable(ParallelScope.Children)]

namespace Content.IntegrationTests;

[SetUpFixture]
public sealed class PoolManagerTestEventHandler
{
    [OneTimeTearDown]
    public void TearDown()
    {
        PoolManager.Shutdown();
    }
}
