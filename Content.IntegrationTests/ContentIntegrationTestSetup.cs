using NUnit.Framework;

[SetUpFixture]
public class ContentIntegrationTestSetup
{
    [OneTimeTearDown]
    public void TearDown()
    {
        var robustSetup = new RobustIntegrationTestSetup();

        robustSetup.Shutdown();
        robustSetup.PrintTestPoolingInfo();
    }
}
