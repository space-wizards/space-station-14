using System;
using System.Threading.Tasks;
using NUnit.Framework;

[assembly: Parallelizable(ParallelScope.Children)]

namespace Content.IntegrationTests;

[SetUpFixture]
public sealed class PoolManagerTestEventHandler
{
    // This value is double the usual time for Content Integration tests
    private static TimeSpan MaximumTotalTestingTimeLimit => TimeSpan.FromMinutes(7);
    private static TimeSpan HardStopTimeLimit => MaximumTotalTestingTimeLimit.Add(TimeSpan.FromMinutes(1));
    [OneTimeSetUp]
    public void Setup()
    {
        // If the tests seem to be stuck, we try to end it semi-nicely
        _ = Task.Delay(MaximumTotalTestingTimeLimit).ContinueWith(_ =>
        {
            PoolManager.Shutdown();
        });

        // If ending it nicely doesn't work within a minute, we do something a bit meaner.
        _ = Task.Delay(HardStopTimeLimit).ContinueWith(_ =>
        {
            var deathReport = PoolManager.DeathReport();
            Environment.FailFast($"Tests took way too ;\n Death Report:\n{deathReport}");
        });
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        PoolManager.Shutdown();
    }
}
