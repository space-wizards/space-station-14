#nullable enable
using System.Collections.Generic;
using Content.Client.GameTicking.Managers;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.NUnit.Constraints;
using Content.Server.GameTicking;
using Content.Shared.GameTicking;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.GameTestTests;

[TestOf(typeof(GameTest))]
[TestOf(typeof(SidedDependencyAttribute))]
public sealed class DependencyTests : GameTest
{
    [SidedDependency(Side.Server)] private readonly SharedGameTicker _sGameTicker = null!;
    [SidedDependency(Side.Client)] private readonly SharedGameTicker _cGameTicker = null!;
    [SidedDependency(Side.Server)] private readonly EntityQuery<TransformComponent> _sXformQuery = default!;

    [Test]
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

    [Test]
    [Description("Asserts that query dependencies function")]
    public async Task QueryDependencies()
    {
        var ent = await Spawn(null);

        Assert.That(_sXformQuery.HasComp(ent), Is.True);
    }
}
