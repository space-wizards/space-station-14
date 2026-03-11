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

}
