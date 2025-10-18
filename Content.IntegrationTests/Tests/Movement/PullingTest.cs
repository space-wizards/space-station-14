#nullable enable
using Content.Shared.Alert;
using Content.Shared.Input;
using Content.Shared.Movement.Pulling.Components;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests.Movement;

public sealed class PullingTest : MovementTest
{
    protected override int Tiles => 4;

    [Test]
    public async Task PullTest()
    {
        var cAlert = Client.System<AlertsSystem>();
        var sAlert = Server.System<AlertsSystem>();
        await SpawnTarget("MobHuman");

        var puller = Comp<PullerComponent>(Player);
        var pullable = Comp<PullableComponent>(Target);

        // Player is initially to the left of the target and not pulling anything
        Assert.That(Delta(), Is.InRange(0.9f, 1.1f));
        Assert.That(puller.Pulling, Is.Null);
        Assert.That(pullable.Puller, Is.Null);
        Assert.That(pullable.BeingPulled, Is.False);
        Assert.That(cAlert.IsShowingAlert(CPlayer, puller.PullingAlert), Is.False);
        Assert.That(sAlert.IsShowingAlert(SPlayer, puller.PullingAlert), Is.False);

        // Start pulling
        await PressKey(ContentKeyFunctions.TryPullObject);
        await RunTicks(5);
        Assert.That(puller.Pulling, Is.EqualTo(STarget));
        Assert.That(pullable.Puller, Is.EqualTo(SPlayer));
        Assert.That(pullable.BeingPulled, Is.True);
        Assert.That(cAlert.IsShowingAlert(CPlayer, puller.PullingAlert), Is.True);
        Assert.That(sAlert.IsShowingAlert(SPlayer, puller.PullingAlert), Is.True);

        // Move to the left and check that the target moves with the player and is still being pulled.
        await Move(DirectionFlag.West, 1);
        Assert.That(Delta(), Is.InRange(0.9f, 1.3f));
        Assert.That(puller.Pulling, Is.EqualTo(STarget));
        Assert.That(pullable.Puller, Is.EqualTo(SPlayer));
        Assert.That(pullable.BeingPulled, Is.True);
        Assert.That(cAlert.IsShowingAlert(CPlayer, puller.PullingAlert), Is.True);
        Assert.That(sAlert.IsShowingAlert(SPlayer, puller.PullingAlert), Is.True);

        // Move in the other direction
        await Move(DirectionFlag.East, 2);
        Assert.That(Delta(), Is.InRange(-1.3f, -0.9f));
        Assert.That(puller.Pulling, Is.EqualTo(STarget));
        Assert.That(pullable.Puller, Is.EqualTo(SPlayer));
        Assert.That(pullable.BeingPulled, Is.True);
        Assert.That(cAlert.IsShowingAlert(CPlayer, puller.PullingAlert), Is.True);
        Assert.That(sAlert.IsShowingAlert(SPlayer, puller.PullingAlert), Is.True);

        // Stop pulling
        await PressKey(ContentKeyFunctions.ReleasePulledObject);
        await RunTicks(5);
        Assert.That(Delta(), Is.InRange(-1.3f, -0.9f));
        Assert.That(puller.Pulling, Is.Null);
        Assert.That(pullable.Puller, Is.Null);
        Assert.That(pullable.BeingPulled, Is.False);
        Assert.That(cAlert.IsShowingAlert(CPlayer, puller.PullingAlert), Is.False);
        Assert.That(sAlert.IsShowingAlert(SPlayer, puller.PullingAlert), Is.False);

        // Move back to the left and ensure the target is no longer following us.
        await Move(DirectionFlag.West, 2);
        Assert.That(Delta(), Is.GreaterThan(2f));
    }
}

