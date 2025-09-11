using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Doors.Systems;
using Content.Shared.Doors.Components;

namespace Content.IntegrationTests.Tests.Doors;

public sealed class AirlockPryingTest : InteractionTest
{
    [Test]
    public async Task PoweredClosedAirlock_Pry_DoesNotOpen()
    {
        await SpawnTarget(Airlock);
        await SpawnEntity("APCBasic", SEntMan.GetCoordinates(TargetCoords));

        await RunTicks(1);

        Assert.That(TryComp<AirlockComponent>(out var airlockComp), "Airlock does not have AirlockComponent?");
        Assert.That(airlockComp.Powered, "Airlock should be powered for this test.");

        Assert.That(TryComp<DoorComponent>(out var doorComp), "Airlock does not have DoorComponent?");
        Assert.That(doorComp.State, Is.EqualTo(DoorState.Closed), "Airlock did not start closed.");

        await InteractUsing(Pry);

        Assert.That(doorComp.State, Is.EqualTo(DoorState.Closed), "Powered airlock was pried open.");
    }

    [Test]
    public async Task PoweredOpenAirlock_Pry_DoesNotClose()
    {
        await SpawnTarget(Airlock);
        await SpawnEntity("APCBasic", SEntMan.GetCoordinates(TargetCoords));

        await RunTicks(1);

        Assert.That(TryComp<AirlockComponent>(out var airlockComp), "Airlock does not have AirlockComponent?");
        Assert.That(airlockComp.Powered, "Airlock should be powered for this test.");

        var doorSys = SEntMan.System<DoorSystem>();
        await Server.WaitPost(() => doorSys.SetState(SEntMan.GetEntity(Target.Value), DoorState.Open));

        Assert.That(TryComp<DoorComponent>(out var doorComp), "Airlock does not have DoorComponent?");
        Assert.That(doorComp.State, Is.EqualTo(DoorState.Open), "Airlock did not start open.");

        await InteractUsing(Pry);

        Assert.That(doorComp.State, Is.EqualTo(DoorState.Open), "Powered airlock was pried closed.");
    }

    [Test]
    public async Task UnpoweredClosedAirlock_Pry_Opens()
    {
        await SpawnTarget(Airlock);

        Assert.That(TryComp<AirlockComponent>(out var airlockComp), "Airlock does not have AirlockComponent?");
        Assert.That(airlockComp.Powered, Is.False, "Airlock should not be powered for this test.");

        Assert.That(TryComp<DoorComponent>(out var doorComp), "Airlock does not have DoorComponent?");
        Assert.That(doorComp.State, Is.EqualTo(DoorState.Closed), "Airlock did not start closed.");

        await InteractUsing(Pry);

        Assert.That(doorComp.State, Is.EqualTo(DoorState.Opening), "Unpowered airlock failed to pry open.");
    }

    [Test]
    public async Task UnpoweredOpenAirlock_Pry_Closes()
    {
        await SpawnTarget(Airlock);

        Assert.That(TryComp<AirlockComponent>(out var airlockComp), "Airlock does not have AirlockComponent?");
        Assert.That(airlockComp.Powered, Is.False, "Airlock should not be powered for this test.");

        var doorSys = SEntMan.System<DoorSystem>();
        await Server.WaitPost(() => doorSys.SetState(SEntMan.GetEntity(Target.Value), DoorState.Open));

        Assert.That(TryComp<DoorComponent>(out var doorComp), "Airlock does not have DoorComponent?");
        Assert.That(doorComp.State, Is.EqualTo(DoorState.Open), "Airlock did not start open.");

        await InteractUsing(Pry);

        Assert.That(doorComp.State, Is.EqualTo(DoorState.Closing), "Unpowered airlock failed to pry closed.");
    }
}
