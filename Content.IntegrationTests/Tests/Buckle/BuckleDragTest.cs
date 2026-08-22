#nullable enable
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Input;
using Content.Shared.Movement.Pulling.Components;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Buckle;

public sealed class BuckleDragTest : InteractionTest
{
    private static readonly EntProtoId TestMob = "MobHuman";
    private static readonly EntProtoId Chair = "Chair";

    [SidedDependency(Side.Server)] private SharedBuckleSystem _sBuckleSystem = default!;

    [Test]
    [Description("Checks that dragging a buckled player unbuckles them.")]
    public async Task BucklePullTest()
    {
        var urist = await SpawnTarget(TestMob);
        var sUrist = ToServer(urist);
        await SpawnTarget(Chair);

        var buckle = Comp<BuckleComponent>(urist);
        var strap = Comp<StrapComponent>(Target);
        var puller = Comp<PullerComponent>(Player);
        var pullable = Comp<PullableComponent>(urist);

#pragma warning disable RA0002
        buckle.Delay = TimeSpan.Zero;
#pragma warning restore RA0002

        using (Assert.EnterMultipleScope())
        {
            // Initially not buckled to the chair and not pulling anything
            Assert.That(buckle.Buckled, Is.False);
            Assert.That(buckle.BuckledTo, Is.Null);
            Assert.That(strap.BuckledEntities, Is.Empty);
            Assert.That(puller.Pulling, Is.Null);
            Assert.That(pullable.Puller, Is.Null);
            Assert.That(pullable.BeingPulled, Is.False);
        }

        // Strap the human to the chair
        await Server.WaitAssertion(() =>
        {
            Assert.That(_sBuckleSystem.TryBuckle(sUrist, SPlayer, STarget.Value));
        });

        await RunTicksSync(5);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(buckle.Buckled, Is.True);
            Assert.That(buckle.BuckledTo, Is.EqualTo(STarget));
            Assert.That(strap.BuckledEntities, Is.EquivalentTo([sUrist]));
            Assert.That(puller.Pulling, Is.Null);
            Assert.That(pullable.Puller, Is.Null);
            Assert.That(pullable.BeingPulled, Is.False);
        }

        // Start pulling, and thus unbuckle them
        await PressKey(ContentKeyFunctions.TryPullObject, cursorEntity: urist);
        await RunTicks(5);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(buckle.Buckled, Is.False);
            Assert.That(buckle.BuckledTo, Is.Null);
            Assert.That(strap.BuckledEntities, Is.Empty);
            Assert.That(puller.Pulling, Is.EqualTo(sUrist));
            Assert.That(pullable.Puller, Is.EqualTo(SPlayer));
            Assert.That(pullable.BeingPulled, Is.True);
        }
    }
}
