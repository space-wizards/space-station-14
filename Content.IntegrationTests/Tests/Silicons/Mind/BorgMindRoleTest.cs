using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Mind;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Silicons.Mind;

public sealed class BorgMindRoleTest : InteractionTest
{
    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  id: BorgChassisGenericProvider
  parent: BaseBorgChassisNT
  name: crewsimov-provider cyborg
  suffix: Debug, Provider
  components:
  - type: Sprite
    layers:
    - state: robot
      map: [""enum.BorgVisualLayers.Body"", ""movement""]
    - state: robot_e_r
      map: [""enum.BorgVisualLayers.Light""]
      shader: unshaded
      visible: false
    - state: robot_l
      shader: unshaded
      map: [""light"",""enum.BorgVisualLayers.LightStatus""]
      visible: false
  - type: BorgChassis
    hasMindState: robot_e
    noMindState: robot_e_r
  - type: BorgTransponder
    sprite:
      sprite: Mobs/Silicon/chassis.rsi
      state: robot
    name: cyborg
  - type: SiliconLawProvider
    laws: Crewsimov
";

    private static readonly EntProtoId MmiProto = "MMI";
    private static readonly EntProtoId MmiIonStormedProto = "MMIIonStormed";
    private static readonly EntProtoId BrainProto = "OrganHumanBrain";
    private static readonly EntProtoId PositronicBrainProto = "PositronicBrain";
    private static readonly EntProtoId PositronicBrainIonStormedProto = "PositronicBrainIonStormed";
    private static readonly EntProtoId BorgChassisGenericProto = "BorgChassisGeneric";
    private static readonly EntProtoId BorgChassisGenericProviderProto = "BorgChassisGenericProvider";
    private static readonly EntProtoId SyndicateAssaultBorgChassisDerelictProto = "SyndicateAssaultBorgChassisDerelict";

    [Test]
    [TestOf(typeof(MindSystem))]
    [Description("Ensures that mind gets proper sub-role when becoming silicon.")]
    public async Task TestBrainMindRoleMMI()
    {
        // Test with non-subverted MMI
        var mindSystem = SEntMan.System<MindSystem>();
        var roleSystem = SEntMan.System<SharedRoleSystem>();
        var containerSystem = SEntMan.System<SharedContainerSystem>();

        EntityUid mmi = default;
        EntityUid brain = default;
        EntityUid mindId = default;

        await Server.WaitPost(() =>
        {
            mmi = SEntMan.SpawnEntity(MmiProto, MapCoordinates.Nullspace);
            brain = SEntMan.SpawnEntity(BrainProto, MapCoordinates.Nullspace);
            var mind = mindSystem.CreateMind(null, "TestMind");
            mindSystem.TransferTo(mind, brain, true);
            mindId = mind;
        });

        var mmiComponent = SEntMan.GetComponent<MMIComponent>(mmi);

        Assert.That(!roleSystem.MindHasRole<SiliconBrainRoleComponent>(mindId),
            $"Mind was silicon before being inserted into MMI. Mind ID: {mindId}.");

        if (mmiComponent.BrainSlot.ID == null)
        {
            Assert.Fail("MMI missing brain before insertion.");
            return;
        }

        var container = containerSystem.GetContainer(mmi, mmiComponent.BrainSlot.ID);

        await Server.WaitAssertion(() =>
            Assert.That(containerSystem.Insert(brain, container, force: true), "Failed to insert brain in the MMI.")
        );


        Assert.That(container.Count, Is.GreaterThan(0), "Container was empty after insertion");

        await Server.WaitRunTicks(15);

        await Server.WaitAssertion(() =>
            Assert.That(roleSystem.MindHasRole<SiliconBrainRoleComponent>(mindId),
                    $"Mind should have been silicon, but wasn't.")
        );

        if (mmiComponent.BrainSlot.ID == null)
        {
            Assert.Fail("MMI missing brain after insertion.");
            return;
        }

        await Server.WaitAssertion(() =>
            containerSystem.Remove(brain, container)
        );

        await Server.WaitRunTicks(15);


        await Server.WaitAssertion(() =>
            Assert.That(!roleSystem.MindHasRole<SiliconBrainRoleComponent>(mindId),
                    $"Mind should not be silicon but was.")
        );

        await Server.WaitPost(() =>
        {
            SEntMan.DeleteEntity(mmi);
            SEntMan.DeleteEntity(brain);
            SEntMan.DeleteEntity(mindId);
        });
    }

    [Test]
    [TestOf(typeof(MindSystem))]
    [Description("Ensures that mind gets proper sub-role when becoming altered silicon.")]
    public async Task TestBrainMindRoleMMIIonStormed()
    {
        // Test with subverted MMI
        var mindSystem = SEntMan.System<MindSystem>();
        var roleSystem = SEntMan.System<SharedRoleSystem>();
        var containerSystem = SEntMan.System<SharedContainerSystem>();

        EntityUid mmi = default;
        EntityUid brain = default;
        EntityUid mindId = default;

        await Server.WaitPost(() =>
        {
            mmi = SEntMan.SpawnEntity(MmiIonStormedProto, MapCoordinates.Nullspace);
            brain = SEntMan.SpawnEntity(BrainProto, MapCoordinates.Nullspace);
            var mind = mindSystem.CreateMind(null, "TestMind");
            mindSystem.TransferTo(mind, brain, true);
            mindId = mind;
        });

        // ion storm component is server-side
        await Server.WaitRunTicks(15);

        var mmiComponent = SEntMan.GetComponent<MMIComponent>(mmi);

        Assert.That(!roleSystem.MindHasRole<SubvertedSiliconRoleComponent>(mindId),
            $"Mind was subverted before being inserted into MMI. Mind ID: {mindId}.");

        if (mmiComponent.BrainSlot.ID == null)
        {
            Assert.Fail("MMI missing brain before insertion.");
            return;
        }

        var container = containerSystem.GetContainer(mmi, mmiComponent.BrainSlot.ID);

        await Server.WaitAssertion(() =>
            Assert.That(containerSystem.Insert(brain, container, force: true), "Failed to insert brain in the MMI.")
        );


        Assert.That(container.Count, Is.GreaterThan(0), "Container was empty after insertion");

        await Server.WaitRunTicks(15);

        await Server.WaitAssertion(() =>
            Assert.That(roleSystem.MindHasRole<SubvertedSiliconRoleComponent>(mindId),
                    $"Mind should have been Subverted, but wasn't.")
        );

        if (mmiComponent.BrainSlot.ID == null)
        {
            Assert.Fail("MMI missing brain after insertion.");
            return;
        }

        await Server.WaitAssertion(() =>
            containerSystem.Remove(brain, container)
        );

        await Server.WaitRunTicks(15);


        await Server.WaitAssertion(() =>
            Assert.That(!roleSystem.MindHasRole<SubvertedSiliconRoleComponent>(mindId),
                    $"Mind should not be subverted but was.")
        );

        await Server.WaitPost(() =>
        {
            SEntMan.DeleteEntity(mmi);
            SEntMan.DeleteEntity(brain);
            SEntMan.DeleteEntity(mindId);
        });
    }

    [Test]
    [TestOf(typeof(MindSystem))]
    [Description("Tests that mind has proper sub-role when becoming silicon/altered silicon " +
                 "depending on its state when inserted on a provider/non-provided chassis.")]
    public async Task TestBrainMindRoleChassis()
    {
        var mindSystem = SEntMan.System<MindSystem>();
        var roleSystem = SEntMan.System<SharedRoleSystem>();
        var containerSystem = SEntMan.System<SharedContainerSystem>();

        // 1. Inserting ion stormed positronic brain into non-ion stormed and not provider chassis
        // Check if mind still has subverted silicon.
        await Server.WaitAssertion(() =>
        {
            var brain = SEntMan.SpawnEntity(PositronicBrainIonStormedProto, MapCoordinates.Nullspace);
            var chassis = SEntMan.SpawnEntity(BorgChassisGenericProto, MapCoordinates.Nullspace);
            var mind = mindSystem.CreateMind(null, "TestMind");
            mindSystem.TransferTo(mind, brain, true);

            // Brain should be subverted due to ion storm
            Assert.That(roleSystem.MindHasRole<SubvertedSiliconRoleComponent>(mind), "Mind should be subverted in ion stormed brain.");

            var chassisComp = SEntMan.GetComponent<BorgChassisComponent>(chassis);
            containerSystem.EnsureContainer<ContainerSlot>(chassis, chassisComp.BrainContainerId);
            if (chassisComp.BrainEntity is { } contained)
                SEntMan.DeleteEntity(contained);

            containerSystem.Insert(brain, chassisComp.BrainContainer);

            // Should still be subverted because chassis is not a provider
            Assert.That(roleSystem.MindHasRole<SubvertedSiliconRoleComponent>(mind), "Mind should still be subverted after insertion into non-provider chassis.");

            SEntMan.DeleteEntity(chassis);
            SEntMan.DeleteEntity(brain);
            SEntMan.DeleteEntity(mind);
        });

        // 2. Removing positronic brain from ion stormed chassis
        // Check if mind still has subverted silicon (it shouldn't).
        // And inserting non-ion stormed positronic brain into ion stormed provider chassis
        // See if mind has subverted silicon (it should).

        EntityUid chassis2 = default;
        EntityUid brain2 = default;
        EntityUid mind2 = default;

        await Server.WaitPost(() =>
        {
            chassis2 = SEntMan.SpawnEntity(SyndicateAssaultBorgChassisDerelictProto, MapCoordinates.Nullspace);
            brain2 = SEntMan.SpawnEntity(PositronicBrainProto, MapCoordinates.Nullspace);
            mind2 = mindSystem.CreateMind(null, "TestMind");
            mindSystem.TransferTo(mind2, brain2, true);
        });

        // Wait for ion storm on chassis
        await Server.WaitRunTicks(15);

        await Server.WaitAssertion(() =>
        {
            Assert.That(!roleSystem.MindHasRole<SubvertedSiliconRoleComponent>(mind2), "Mind should not be subverted initially.");

            var chassisComp = SEntMan.GetComponent<BorgChassisComponent>(chassis2);
            containerSystem.EnsureContainer<ContainerSlot>(chassis2, chassisComp.BrainContainerId);
            if (chassisComp.BrainEntity is { } contained)
                SEntMan.DeleteEntity(contained);

            containerSystem.Insert(brain2, chassisComp.BrainContainer);

            // Chassis is ion stormed provider, so the mind should become subverted
            Assert.That(roleSystem.MindHasRole<SubvertedSiliconRoleComponent>(mind2), "Mind should be subverted after insertion into ion stormed provider chassis.");

            // Now remove
            containerSystem.Remove(brain2, chassisComp.BrainContainer);

            // Mind should NOT be subverted anymore
            Assert.That(!roleSystem.MindHasRole<SubvertedSiliconRoleComponent>(mind2), "Mind should not be subverted after removal from ion stormed chassis.");
        });

        await Server.WaitPost(() =>
        {
            SEntMan.DeleteEntity(chassis2);
            SEntMan.DeleteEntity(brain2);
            SEntMan.DeleteEntity(mind2);
        });

        // 3. Inserting ion stormed positronic brain into a non-subverted provider chassis
        // See if mind doesn't have subverted silicon (it shouldn't have subverted a mind role).
        EntityUid chassis3 = default;
        EntityUid brain3 = default;
        EntityUid mind3 = default;

        await Server.WaitPost(() =>
        {
            brain3 = SEntMan.SpawnEntity(PositronicBrainIonStormedProto, MapCoordinates.Nullspace);

            // We need a non-subverted provider chassis.
            chassis3 = SEntMan.SpawnEntity(BorgChassisGenericProviderProto, MapCoordinates.Nullspace);

            mind3 = mindSystem.CreateMind(null, "TestMind");
            mindSystem.TransferTo(mind3, brain3, true);
        });

        // Wait for ion storm on the brain
        await Server.WaitRunTicks(15);

        await Server.WaitAssertion(() =>
        {
            Assert.That(roleSystem.MindHasRole<SubvertedSiliconRoleComponent>(mind3), "Mind should be subverted in ion stormed brain.");

            var chassisComp = SEntMan.GetComponent<BorgChassisComponent>(chassis3);
            containerSystem.EnsureContainer<ContainerSlot>(chassis3, chassisComp.BrainContainerId);
            if (chassisComp.BrainEntity is { } contained)
                SEntMan.DeleteEntity(contained);

            containerSystem.Insert(brain3, chassisComp.BrainContainer);

            // Chassis is a provider and NOT subverted. It should override the brain's subversion.
            Assert.That(!roleSystem.MindHasRole<SubvertedSiliconRoleComponent>(mind3), "Mind should NOT be subverted after insertion into non-subverted provider chassis.");
        });

        await Server.WaitPost(() =>
        {
            SEntMan.DeleteEntity(chassis3);
            SEntMan.DeleteEntity(brain3);
            SEntMan.DeleteEntity(mind3);
        });
    }
}
