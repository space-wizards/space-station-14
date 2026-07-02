#nullable enable
using System.Linq;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Lock;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Silicons;

public sealed class BorgTests : InteractionTest
{
    private static readonly EntProtoId GenericBorgId = "BorgChassisGeneric";
    private static readonly EntProtoId ModuleId1 = "BorgModuleCable";
    private static readonly EntProtoId ModuleId2 = "BorgModuleFireExtinguisher";
    private static readonly EntProtoId BorgWirepanelToolId = "Crowbar";

    /// <summary>
    /// Tests inserting modules into borgs.
    /// </summary>
    [Test]
    public async Task BorgModuleInsertionTest()
    {
        // Spawn a borg
        await SpawnTarget(GenericBorgId);
        var chassisComp = Comp<BorgChassisComponent>(Target);

        // Spawn a module in the player's hand.
        var module = await PlaceInHands(ModuleId1);

        // Try to insert the module, which should fail due to the borg being locked.
        await Interact();
        Assert.That(HandSys.IsHolding(SPlayer, ToServer(module)), "Inserted a borg module into a locked borg.");

        // Unlock the borg.
        var lockSys = SEntMan.System<LockSystem>();
        await Server.WaitPost(() => lockSys.Unlock(STarget.Value, null));
        Assert.That(lockSys.IsLocked(STarget.Value), Is.False, "Unable to unlock borg.");

        // Try to insert the module, which should fail due to the wire panel being closed.
        await Interact();
        Assert.That(HandSys.IsHolding(SPlayer, ToServer(module)), "Inserted a borg module into a borg with closed wire panel.");

        // Open the wire panel with the correct tool.
        await PlaceInHands(BorgWirepanelToolId);
        await Interact();

        // Spawn a new module in the player's hand and insert it into the borg.
        module = await PlaceInHands(ModuleId1);
        await Interact();
        Assert.That(HandSys.IsHolding(SPlayer, ToServer(module)), Is.False, "Unable to insert borg module.");
        Assert.That(chassisComp.ModuleContainer.ContainedEntities.ToList(), Does.Contain(ToServer(module)), "Borg module was not inserted into module container.");

        // Try inserting a second module of the same type, which should fail.
        module = await PlaceInHands(ModuleId1);
        await Interact();
        Assert.That(HandSys.IsHolding(SPlayer, ToServer(module)), "Inserted a duplicate borg module.");
        Assert.That(chassisComp.ModuleContainer.ContainedEntities.ToList(), Does.Not.Contain(ToServer(module)), "Inserted a duplicate borg module.");

        // Try inserting a different module type, which should succeed.
        module = await PlaceInHands(ModuleId2);
        await Interact();
        Assert.That(HandSys.IsHolding(SPlayer, ToServer(module)), Is.False, "Unable to insert borg module.");
        Assert.That(chassisComp.ModuleContainer.ContainedEntities.ToList(), Does.Contain(ToServer(module)), "Borg module was not inserted into module container.");
    }
}
