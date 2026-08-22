#nullable enable
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.Buckle.Components;
using Content.Shared.Interaction;

namespace Content.IntegrationTests.Tests.Buckle;

public sealed partial class BuckleTest
{
    [SidedDependency(Side.Server)] private SharedInteractionSystem _sInteraction = null!;

    [Test]
    [RunOnSide(Side.Server)]
    public async Task BuckleInteractUnbuckleOther()
    {
        var user = SSpawn(BuckleDummyId);
        var victim = SSpawn(BuckleDummyId);
        var chair = SSpawn(StrapDummyId);

        Assert.That(SEntMan.TryGetComponent<BuckleComponent>(victim, out var buckle));
        Assert.That(SEntMan.TryGetComponent<StrapComponent>(chair, out var strap));

#pragma warning disable RA0002
        buckle!.Delay = TimeSpan.Zero;
#pragma warning restore RA0002

        // Buckle victim to chair
        Assert.That(_sBuckle.TryBuckle(victim, user, chair, buckle));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(buckle.BuckledTo, Is.EqualTo(chair), "Victim did not get buckled to the chair.");
            Assert.That(buckle.Buckled, "Victim is not buckled.");
            Assert.That(strap!.BuckledEntities, Does.Contain(victim), "Chair does not have victim buckled to it.");
        }

        // InteractHand with chair to unbuckle victim
        _sInteraction.InteractHand(user, chair);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(buckle.BuckledTo, Is.Null);
            Assert.That(buckle.Buckled, Is.False);
            Assert.That(strap.BuckledEntities, Does.Not.Contain(victim));
        }
    }

    [Test]
    [RunOnSide(Side.Server)]
    public async Task BuckleInteractBuckleUnbuckleSelf()
    {
        var user = SSpawn(BuckleDummyId);
        var chair = SSpawn(StrapDummyId);

        Assert.That(SEntMan.TryGetComponent<BuckleComponent>(user, out var buckle));
        Assert.That(SEntMan.TryGetComponent<StrapComponent>(chair, out var strap));

#pragma warning disable RA0002
        buckle!.Delay = TimeSpan.Zero;
#pragma warning restore RA0002

        // Buckle user to chair
        _sInteraction.InteractHand(user, chair);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(buckle.BuckledTo, Is.EqualTo(chair), "Victim did not get buckled to the chair.");
            Assert.That(buckle.Buckled, "Victim is not buckled.");
            Assert.That(strap!.BuckledEntities, Does.Contain(user), "Chair does not have victim buckled to it.");
        }

        // InteractHand with chair to unbuckle
        _sInteraction.InteractHand(user, chair);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(buckle.BuckledTo, Is.Null);
            Assert.That(buckle.Buckled, Is.False);
            Assert.That(strap.BuckledEntities, Does.Not.Contain(user));
        }
    }
}
