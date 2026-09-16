#nullable enable
using System.Threading;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;

namespace Content.IntegrationTests.Tests.GameTestTests;

[TestOf(typeof(GameTest))]
[TestOf(typeof(RunOnSideAttribute))]
[Description("Asserts that RunOnSide actually does as expected and runs the test on the given side.")]
public sealed class RunOnSideTests : GameTest
{
    [Test]
    [Description("Ensures that the default scenario is the test thread.")]
    public void Control()
    {
        Assert.That(Thread.CurrentThread, Is.Not.EqualTo(ServerThread).And.Not.EqualTo(ClientThread));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void TestServerSide()
    {
        Assert.That(Thread.CurrentThread, Is.EqualTo(ServerThread));
    }

    [Test]
    [RunOnSide(Side.Client)]
    public void TestClientSide()
    {
        Assert.That(Thread.CurrentThread, Is.EqualTo(ClientThread));
    }

    [Test]
    [RunOnSide(Side.Server)]
    [Description("Ensures that RunOnSide appropriately adds a property.")]
    [Ignore("TestContext on the game threads is broken.")]
    [TrackingIssue("https://github.com/space-wizards/RobustToolbox/issues/6449")]
    public void TestProperty()
    {
        Assert.That(TestContext.CurrentContext.Test.Properties.Get(RunOnSideAttribute.RunOnSideProperty), Is.Not.Null);
    }
}
