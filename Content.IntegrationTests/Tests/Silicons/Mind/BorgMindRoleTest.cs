using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Mind;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.Silicons.Mind;

[TestFixture]
public sealed class BorgMindRoleTest : InteractionTest
{
    [Test]
    public async Task TestBrainMindRoleMMI()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        // Test with non-subverted MMI
        var mmiPrototype = "MMI";
        var brainPrototype = "OrganHumanBrain";
        var entManager = server.EntMan;
        var mindSystem = entManager.System<MindSystem>();
        var roleSystem = entManager.System<SharedRoleSystem>();
        var containerSystem = entManager.System<SharedContainerSystem>();

        EntityUid mmi = default;
        EntityUid brain = default;
        EntityUid mindId = default;

        await server.WaitPost(() =>
        {
            mmi = entManager.SpawnEntity(mmiPrototype, MapCoordinates.Nullspace);
            brain = entManager.SpawnEntity(brainPrototype, MapCoordinates.Nullspace);
            var mind = mindSystem.CreateMind(null, "TestMind");
            mindSystem.TransferTo(mind, brain, true);
            mindId = mind;
        });

        var mmiComponent = entManager.GetComponent<MMIComponent>(mmi);

        Assert.That(!roleSystem.MindHasRole<SiliconBrainRoleComponent>(mindId),
            $"Mind was silicon before being inserted into MMI. Mind ID: {mindId}.");

        if (mmiComponent.BrainSlot.ID == null)
        {
            Assert.Fail("MMI missing brain before insertion.");
            return;
        }

        var container = containerSystem.GetContainer(mmi, mmiComponent.BrainSlot.ID);

        await server.WaitAssertion(() =>
            Assert.That(containerSystem.Insert(brain, container, force: true), "Failed to insert brain in the MMI.")
        );


        Assert.That(container.Count, Is.GreaterThan(0), "Container was empty after insertion");

        await server.WaitRunTicks(15);

        await server.WaitAssertion(() =>
            Assert.That(roleSystem.MindHasRole<SiliconBrainRoleComponent>(mindId),
                    $"Mind should have been silicon, but wasn't.")
        );

        if (mmiComponent.BrainSlot.ID == null)
        {
            Assert.Fail("MMI missing brain after insertion.");
            return;
        }

        await server.WaitAssertion(() =>
            containerSystem.Remove(brain, container)
        );

        await server.WaitRunTicks(15);


        await server.WaitAssertion(() =>
            Assert.That(!roleSystem.MindHasRole<SiliconBrainRoleComponent>(mindId),
                    $"Mind should not be silicon but was.")
        );

        await server.WaitPost(() =>
        {
            entManager.DeleteEntity(mmi);
            entManager.DeleteEntity(brain);
            entManager.DeleteEntity(mindId);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task TestBrainMindRoleMMIIonStormed()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        // Test with subverted MMI
        var mmiPrototype = "MMIIonStormed";
        var brainPrototype = "OrganHumanBrain";
        var entManager = server.EntMan;
        var mindSystem = entManager.System<MindSystem>();
        var roleSystem = entManager.System<SharedRoleSystem>();
        var containerSystem = entManager.System<SharedContainerSystem>();

        EntityUid mmi = default;
        EntityUid brain = default;
        EntityUid mindId = default;

        await server.WaitPost(() =>
        {
            mmi = entManager.SpawnEntity(mmiPrototype, MapCoordinates.Nullspace);
            brain = entManager.SpawnEntity(brainPrototype, MapCoordinates.Nullspace);
            var mind = mindSystem.CreateMind(null, "TestMind");
            mindSystem.TransferTo(mind, brain, true);
            mindId = mind;
        });

        // ion storm component is server-side
        await server.WaitRunTicks(15);

        var mmiComponent = entManager.GetComponent<MMIComponent>(mmi);

        Assert.That(!roleSystem.MindHasRole<SubvertedSiliconRoleComponent>(mindId),
            $"Mind was subverted before being inserted into MMI. Mind ID: {mindId}.");

        if (mmiComponent.BrainSlot.ID == null)
        {
            Assert.Fail("MMI missing brain before insertion.");
            return;
        }

        var container = containerSystem.GetContainer(mmi, mmiComponent.BrainSlot.ID);

        await server.WaitAssertion(() =>
            Assert.That(containerSystem.Insert(brain, container, force: true), "Failed to insert brain in the MMI.")
        );


        Assert.That(container.Count, Is.GreaterThan(0), "Container was empty after insertion");

        await server.WaitRunTicks(15);

        await server.WaitAssertion(() =>
            Assert.That(roleSystem.MindHasRole<SubvertedSiliconRoleComponent>(mindId),
                    $"Mind should have been Subverted, but wasn't.")
        );

        if (mmiComponent.BrainSlot.ID == null)
        {
            Assert.Fail("MMI missing brain after insertion.");
            return;
        }

        await server.WaitAssertion(() =>
            containerSystem.Remove(brain, container)
        );

        await server.WaitRunTicks(15);


        await server.WaitAssertion(() =>
            Assert.That(!roleSystem.MindHasRole<SubvertedSiliconRoleComponent>(mindId),
                    $"Mind should not be subverted but was.")
        );

        await server.WaitPost(() =>
        {
            entManager.DeleteEntity(mmi);
            entManager.DeleteEntity(brain);
            entManager.DeleteEntity(mindId);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task TestBrainMindRoleChassis()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entManager = server.EntMan;
        var mindSystem = entManager.System<MindSystem>();
        var roleSystem = entManager.System<SharedRoleSystem>();
        var containerSystem = entManager.System<SharedContainerSystem>();

        // 1. Inserting ion stormed positronic brain into non-ion stormed and not provider chassis
        // Check if mind still has subverted silicon.
        await server.WaitAssertion(() =>
        {
            var brain = entManager.SpawnEntity("PositronicBrainIonStormed", MapCoordinates.Nullspace);
            var chassis = entManager.SpawnEntity("BorgChassisGeneric", MapCoordinates.Nullspace);
            var mind = mindSystem.CreateMind(null, "TestMind");
            mindSystem.TransferTo(mind, brain, true);

            // Brain should be subverted due to ion storm
            Assert.That(roleSystem.MindHasRole<SubvertedSiliconRoleComponent>(mind), "Mind should be subverted in ion stormed brain.");

            var chassisComp = entManager.GetComponent<BorgChassisComponent>(chassis);
            containerSystem.EnsureContainer<ContainerSlot>(chassis, chassisComp.BrainContainerId);
            if (chassisComp.BrainEntity is { } contained)
                entManager.DeleteEntity(contained);

            containerSystem.Insert(brain, chassisComp.BrainContainer);

            // Should still be subverted because chassis is not a provider
            Assert.That(roleSystem.MindHasRole<SubvertedSiliconRoleComponent>(mind), "Mind should still be subverted after insertion into non-provider chassis.");

            entManager.DeleteEntity(chassis);
            entManager.DeleteEntity(brain);
            entManager.DeleteEntity(mind);
        });

        // 2. Removing positronic brain from ion stormed chassis
        // Check if mind still has subverted silicon (it shouldn't).
        // And inserting non-ion stormed positronic brain into ion stormed provider chassis
        // See if mind has subverted silicon (it should).
        await server.WaitPost(() => { }); // Sync

        var chassisProtoIon = "SyndicateAssaultBorgChassisDerelict";

        EntityUid chassis2 = default;
        EntityUid brain2 = default;
        EntityUid mind2 = default;

        await server.WaitPost(() =>
        {
            chassis2 = entManager.SpawnEntity(chassisProtoIon, MapCoordinates.Nullspace);
            brain2 = entManager.SpawnEntity("PositronicBrain", MapCoordinates.Nullspace);
            mind2 = mindSystem.CreateMind(null, "TestMind");
            mindSystem.TransferTo(mind2, brain2, true);
        });

        // Wait for ion storm on chassis
        await server.WaitRunTicks(15);

        await server.WaitAssertion(() =>
        {
            Assert.That(!roleSystem.MindHasRole<SubvertedSiliconRoleComponent>(mind2), "Mind should not be subverted initially.");

            var chassisComp = entManager.GetComponent<BorgChassisComponent>(chassis2);
            containerSystem.EnsureContainer<ContainerSlot>(chassis2, chassisComp.BrainContainerId);
            if (chassisComp.BrainEntity is { } contained)
                entManager.DeleteEntity(contained);

            containerSystem.Insert(brain2, chassisComp.BrainContainer);

            // Chassis is ion stormed provider, so the mind should become subverted
            Assert.That(roleSystem.MindHasRole<SubvertedSiliconRoleComponent>(mind2), "Mind should be subverted after insertion into ion stormed provider chassis.");

            // Now remove
            containerSystem.Remove(brain2, chassisComp.BrainContainer);

            // Mind should NOT be subverted anymore
            Assert.That(!roleSystem.MindHasRole<SubvertedSiliconRoleComponent>(mind2), "Mind should not be subverted after removal from ion stormed chassis.");
        });

        await server.WaitPost(() =>
        {
            entManager.DeleteEntity(chassis2);
            entManager.DeleteEntity(brain2);
            entManager.DeleteEntity(mind2);
        });

        // 3. Inserting ion stormed positronic brain into a non-subverted provider chassis
        // See if mind doesn't have subverted silicon (it shouldn't have subverted a mind role).
        EntityUid chassis3 = default;
        EntityUid brain3 = default;
        EntityUid mind3 = default;

        await server.WaitPost(() =>
        {
            brain3 = entManager.SpawnEntity("PositronicBrainIonStormed", MapCoordinates.Nullspace);

            // We need a non-subverted provider chassis.
            chassis3 = entManager.SpawnEntity("BorgChassisGenericProvider", MapCoordinates.Nullspace);

            mind3 = mindSystem.CreateMind(null, "TestMind");
            mindSystem.TransferTo(mind3, brain3, true);
        });

        // Wait for ion storm on the brain
        await server.WaitRunTicks(15);

        await server.WaitAssertion(() =>
        {
            Assert.That(roleSystem.MindHasRole<SubvertedSiliconRoleComponent>(mind3), "Mind should be subverted in ion stormed brain.");

            var chassisComp = entManager.GetComponent<BorgChassisComponent>(chassis3);
            containerSystem.EnsureContainer<ContainerSlot>(chassis3, chassisComp.BrainContainerId);
            if (chassisComp.BrainEntity is { } contained)
                entManager.DeleteEntity(contained);

            containerSystem.Insert(brain3, chassisComp.BrainContainer);

            // Chassis is a provider and NOT subverted. It should override the brain's subversion.
            Assert.That(!roleSystem.MindHasRole<SubvertedSiliconRoleComponent>(mind3), "Mind should NOT be subverted after insertion into non-subverted provider chassis.");
        });

        await server.WaitPost(() =>
        {
            entManager.DeleteEntity(chassis3);
            entManager.DeleteEntity(brain3);
            entManager.DeleteEntity(mind3);
        });

        await pair.CleanReturnAsync();
    }
}
