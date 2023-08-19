using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Explosion.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.Payload;

public sealed class ModularGrenadeTests : InteractionTest
{
    public const string Trigger = "TimerTrigger";
    public const string Payload = "ExplosivePayload";

    /// <summary>
    /// Test that a modular grenade can be fully crafted and detonated.
    /// </summary>
    [Test]
    public async Task AssembleAndDetonateGrenade()
    {
        await PlaceInHands(Steel, 5);
        await CraftItem("ModularGrenadeRecipe");
        Target = SEntMan.GetNetEntity(await FindEntity("ModularGrenade"));

        await Drop();
        await Interact(Cable);

        // Insert & remove trigger
        AssertComp<OnUseTimerTriggerComponent>(false);
        await Interact(Trigger);
        AssertComp<OnUseTimerTriggerComponent>();
        await FindEntity(Trigger, LookupFlags.Uncontained, shouldSucceed: false);
        await Interact(Pry);
        AssertComp<OnUseTimerTriggerComponent>(false);

        // Trigger was dropped to floor, not deleted.
        await FindEntity(Trigger, LookupFlags.Uncontained);

        // Re-insert
        await Interact(Trigger);
        AssertComp<OnUseTimerTriggerComponent>();

        // Insert & remove payload.
        await Interact(Payload);
        await FindEntity(Payload, LookupFlags.Uncontained, shouldSucceed: false);
        await Interact(Pry);
        var ent = await FindEntity(Payload, LookupFlags.Uncontained);
        await Delete(ent);

        // successfully insert a second time
        await Interact(Payload);
        ent = await FindEntity(Payload);
        var sys = SEntMan.System<SharedContainerSystem>();
        Assert.That(sys.IsEntityInContainer(ent));

        // Activate trigger.
        await Pickup();
        AssertComp<ActiveTimerTriggerComponent>(false);
        await UseInHand();

        // So uhhh grenades in hands don't destroy themselves when exploding. Maybe that will be fixed eventually.
        await Drop();

        // Wait until grenade explodes
        var timer = Comp<ActiveTimerTriggerComponent>();
        while (timer.TimeRemaining >= 0)
        {
            await RunTicks(10);
        }

        // Grenade has exploded.
        await RunTicks(5);
        AssertDeleted();
    }
}
