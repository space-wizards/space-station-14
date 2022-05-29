using NUnit.Framework;

[SetUpFixture]
// ReSharper disable once CheckNamespace
public sealed class ContentIntegrationTestSetup
{
    [OneTimeTearDown]
    public void TearDown()
    {
        var robustSetup = new RobustIntegrationTestSetup();

        robustSetup.Shutdown();
        robustSetup.PrintTestPoolingInfo();
    }
}
