#nullable enable
using System.Collections.Generic;
using Content.Client.GameTicking.Managers;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.NUnit.Constraints;
using Content.Server.GameTicking;
using Content.Shared.GameTicking;

namespace Content.IntegrationTests.Tests.GameTestTests;

[TestOf(typeof(GameTest))]
[TestOf(typeof(SidedDependencyAttribute))]
[TestOf(typeof(SystemAttribute))]
public sealed class DependencyTests : GameTest
{
    [System(Side.Server)] private readonly SharedGameTicker _sGameTicker = null!;
    [System(Side.Client)] private readonly SharedGameTicker _cGameTicker = null!;

    [Test]
    [TestOf(typeof(SidedDependencyAttribute))]
    [Description("Asserts that sided dependencies actually grab from the right sides.")]
    public void DependenciesRespectSides()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(!ReferenceEquals(SEntMan, CEntMan), "Server and client entity managers should be distinct");
            Assert.That(SEntMan, Is.EqualTo(Server.EntMan).Using<object?>(ReferenceEqualityComparer.Instance));
            Assert.That(CEntMan, Is.EqualTo(Client.EntMan).Using<object?>(ReferenceEqualityComparer.Instance));
        }
    }

    [Test]
    [TestOf(typeof(SystemAttribute))]
    [Description("Asserts that system dependencies actually grab from the right sides.")]
    public void SystemDependenciesRespectSides()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(!ReferenceEquals(_sGameTicker, _cGameTicker),
                "Server and client gametickers should be distinct");
            Assert.That(_sGameTicker, Is.TypeOf<GameTicker>());
            Assert.That(_cGameTicker, Is.TypeOf<ClientGameTicker>());
        }
    }
}
