using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Silicons.Laws;

[TestFixture]
public sealed class BorgLawTest : InteractionTest
{
    [Test]
    public async Task TestBorgBrainLawsNonProviderChassis()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entManager = server.EntMan;
        var protoManager = server.ProtoMan;
        var lawSystem = entManager.System<SharedSiliconLawSystem>();
        var containerSystem = entManager.System<SharedContainerSystem>();

        List<EntityPrototype> brainPrototypes = new();
        List<EntityPrototype> chassisPrototypes = new();

        await server.WaitPost(() =>
        {
            brainPrototypes = protoManager.EnumeratePrototypes<EntityPrototype>()
                .Where(p => !p.Abstract && p.Components.ContainsKey("BorgBrain"))
                .ToList();

            chassisPrototypes = protoManager.EnumeratePrototypes<EntityPrototype>()
                .Where(p => !p.Abstract && p.Components.ContainsKey("BorgChassis") && !p.Components.ContainsKey("SiliconLawProvider"))
                .ToList();
        });

        foreach (var brainProto in brainPrototypes)
        {
            foreach (var chassisProto in chassisPrototypes)
            {
                EntityUid brain = default;
                EntityUid chassis = default;
                bool isIonStormed = false;

                await server.WaitPost(() =>
                {
                    brain = entManager.SpawnEntity(brainProto.ID, MapCoordinates.Nullspace);
                    chassis = entManager.SpawnEntity(chassisProto.ID, MapCoordinates.Nullspace);
                    isIonStormed = entManager.HasComponent<StartIonStormedComponent>(brain);
                });

                if (isIonStormed)
                {
                    await pair.RunSeconds(1);
                }

                await server.WaitAssertion(() =>
                {
                    if (!entManager.TryGetComponent<BorgChassisComponent>(chassis, out var chassisComp))
                    {
                         Assert.Fail($"Chassis {chassisProto.ID} missing BorgChassisComponent");
                         return;
                    }

                    containerSystem.EnsureContainer<ContainerSlot>(chassis, chassisComp.BrainContainerId);
                    if (chassisComp.BrainEntity is { } contained)
                        entManager.DeleteEntity(contained);

                    // 1. Check initial brain laws
                    var brainLaws = lawSystem.GetBoundLaws(brain).Laws;

                    // 2. Insert brain into chassis
                    containerSystem.Insert(brain, chassisComp.BrainContainer);

                    // 3. Check if chassis has brain's laws
                    var chassisLaws = lawSystem.GetBoundLaws(chassis).Laws;

                    if (!LawsMatch(brainLaws, chassisLaws))
                    {
                        var brainLawsStr = string.Join(", ", brainLaws.Select(l => l.LawString));
                        var chassisLawsStr = string.Join(", ", chassisLaws.Select(l => l.LawString));
                        Assert.Fail($"Laws do not match for Brain: {brainProto.ID}, Chassis: {chassisProto.ID}.\nBrain Laws: {brainLawsStr}\nChassis Laws: {chassisLawsStr}");
                    }

                    // 4. Remove brain
                    containerSystem.Remove(brain, chassisComp.BrainContainer);

                    // 5. Check if brain still has the same laws
                    var brainLawsAfter = lawSystem.GetBoundLaws(brain).Laws;
                    if (!LawsMatch(brainLaws, brainLawsAfter))
                    {
                        Assert.Fail($"Brain laws changed after removal for Brain: {brainProto.ID}, Chassis: {chassisProto.ID}");
                    }

                    entManager.DeleteEntity(brain);
                    entManager.DeleteEntity(chassis);
                });
            }
        }

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task TestBorgBrainLawsWithProviderChassis()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entManager = server.EntMan;
        var protoManager = server.ProtoMan;
        var lawSystem = entManager.System<SharedSiliconLawSystem>();
        var containerSystem = entManager.System<SharedContainerSystem>();

        List<EntityPrototype> brainPrototypes = new();
        List<EntityPrototype> chassisPrototypes = new();

        await server.WaitPost(() =>
        {
            brainPrototypes = protoManager.EnumeratePrototypes<EntityPrototype>()
                .Where(p => !p.Abstract && p.Components.ContainsKey("BorgBrain"))
                .ToList();

            chassisPrototypes = protoManager.EnumeratePrototypes<EntityPrototype>()
                .Where(p => !p.Abstract && p.Components.ContainsKey("BorgChassis") && p.Components.ContainsKey("SiliconLawProvider"))
                .ToList();
        });

        foreach (var brainProto in brainPrototypes)
        {
            foreach (var chassisProto in chassisPrototypes)
            {
                EntityUid brain = default;
                EntityUid chassis = default;
                bool isIonStormed = false;

                await server.WaitPost(() =>
                {
                    brain = entManager.SpawnEntity(brainProto.ID, MapCoordinates.Nullspace);
                    chassis = entManager.SpawnEntity(chassisProto.ID, MapCoordinates.Nullspace);
                    isIonStormed = entManager.HasComponent<StartIonStormedComponent>(brain);
                });

                if (isIonStormed)
                {
                    await pair.RunSeconds(1);
                }

                await server.WaitAssertion(() =>
                {
                    if (!entManager.TryGetComponent<BorgChassisComponent>(chassis, out var chassisComp))
                    {
                         Assert.Fail($"Chassis {chassisProto.ID} missing BorgChassisComponent");
                         return;
                    }

                    containerSystem.EnsureContainer<ContainerSlot>(chassis, chassisComp.BrainContainerId);
                    if (chassisComp.BrainEntity is { } contained)
                        entManager.DeleteEntity(contained);

                    // 1. Check initial laws
                    var brainLawsInitial = lawSystem.GetBoundLaws(brain).Laws;
                    var chassisLawsInitial = lawSystem.GetBoundLaws(chassis).Laws;

                    // 2. Insert brain into chassis
                    containerSystem.Insert(brain, chassisComp.BrainContainer);

                    // 3. Chassis should be the one telling the laws
                    var chassisLawsAfter = lawSystem.GetBoundLaws(chassis).Laws;
                    if (!LawsMatch(chassisLawsInitial, chassisLawsAfter))
                    {
                         Assert.Fail($"Chassis laws should be its own for Brain: {brainProto.ID}, Chassis: {chassisProto.ID}");
                    }

                    // 4. Remove brain
                    containerSystem.Remove(brain, chassisComp.BrainContainer);

                    // 5. Brain is supposed to have the laws it had before being inserted
                    var brainLawsAfter = lawSystem.GetBoundLaws(brain).Laws;
                    if (!LawsMatch(brainLawsInitial, brainLawsAfter))
                    {
                        Assert.Fail($"Brain laws changed after removal for Brain: {brainProto.ID}, Chassis: {chassisProto.ID}");
                    }

                    entManager.DeleteEntity(brain);
                    entManager.DeleteEntity(chassis);
                });
            }
        }

        await pair.CleanReturnAsync();
    }

    private bool LawsMatch(List<SiliconLaw> laws1, List<SiliconLaw> laws2)
    {
        if (laws1.Count != laws2.Count)
            return false;

        for (int i = 0; i < laws1.Count; i++)
        {
            if (laws1[i].LawString != laws2[i].LawString)
                return false;
            if (laws1[i].Order != laws2[i].Order)
                return false;
            if (laws1[i].LawIdentifierOverride != laws2[i].LawIdentifierOverride)
                return false;
        }

        return true;
    }
}
