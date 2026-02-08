using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Mind;
using Content.Shared.Emag.Systems;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Lock;
using Content.Shared.Wires;
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

    [Test]
    public async Task TestBorgEmag()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entManager = server.EntMan;
        var protoManager = server.ProtoMan;
        var containerSystem = entManager.System<SharedContainerSystem>();
        var emagSystem = entManager.System<EmagSystem>();
        var mindSystem = entManager.System<MindSystem>();
        var lockSystem = entManager.System<LockSystem>();
        var wiresSystem = entManager.System<SharedWiresSystem>();
        var roleSystem = entManager.System<SharedRoleSystem>();

        List<EntityPrototype> brainPrototypes = new();
        List<EntityPrototype> chassisPrototypes = new();

        await server.WaitPost(() =>
        {
            brainPrototypes = protoManager.EnumeratePrototypes<EntityPrototype>()
                .Where(p => !p.Abstract && p.Components.ContainsKey("BorgBrain"))
                .ToList();

            chassisPrototypes = protoManager.EnumeratePrototypes<EntityPrototype>()
                .Where(p => !p.Abstract && p.Components.ContainsKey("BorgChassis"))
                .ToList();
        });

        // Test emagging chassis without a brain
        foreach (var chassisProto in chassisPrototypes)
        {
            EntityUid chassis = default;
            EntityUid user = default;
            EntityUid emag = default;

            await server.WaitPost(() =>
            {
                chassis = entManager.SpawnEntity(chassisProto.ID, MapCoordinates.Nullspace);
                user = entManager.SpawnEntity("MobHuman", MapCoordinates.Nullspace);
                emag = entManager.SpawnEntity("Emag", MapCoordinates.Nullspace);
            });

            await server.WaitAssertion(() =>
            {
                if (!entManager.TryGetComponent<BorgChassisComponent>(chassis, out var chassisComp))
                {
                    Assert.Fail($"Chassis {chassisProto.ID} missing BorgChassisComponent");
                    return;
                }

                // 1. Ensure no brain
                containerSystem.EnsureContainer<ContainerSlot>(chassis, chassisComp.BrainContainerId);
                if (chassisComp.BrainEntity is { } contained)
                    entManager.DeleteEntity(contained);

                var initialLaws = new List<SiliconLaw>();
                if (entManager.TryGetComponent<SiliconLawProviderComponent>(chassis, out var provider))
                    initialLaws.AddRange(provider.Lawset.Laws);

                // 2. Emag chassis without brain
                emagSystem.TryEmagEffect((emag, null), user, chassis, EmagType.Interaction);

                var afterLaws = new List<SiliconLaw>();
                if (entManager.TryGetComponent<SiliconLawProviderComponent>(chassis, out provider))
                    afterLaws.AddRange(provider.Lawset.Laws);

                if (!LawsMatch(initialLaws, afterLaws))
                {
                    Assert.Fail($"Chassis {chassisProto.ID} laws changed after emagging without brain");
                }
            });

            await server.WaitPost(() =>
            {
                entManager.DeleteEntity(chassis);
                entManager.DeleteEntity(user);
                entManager.DeleteEntity(emag);
            });
        }

        // Test emagging chassis with a brain
        foreach (var chassisProto in chassisPrototypes)
        {
            foreach (var brainProto in brainPrototypes)
            {
                EntityUid chassis = default;
                EntityUid brain = default;
                EntityUid user = default;
                EntityUid emag = default;
                bool isIonStormed = false;

                await server.WaitPost(() =>
                {
                    chassis = entManager.SpawnEntity(chassisProto.ID, MapCoordinates.Nullspace);
                    brain = entManager.SpawnEntity(brainProto.ID, MapCoordinates.Nullspace);
                    user = entManager.SpawnEntity("MobHuman", MapCoordinates.Nullspace);
                    emag = entManager.SpawnEntity("Emag", MapCoordinates.Nullspace);
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

                    // 1. Clean out any pre-existing brain from the chassis prototype
                    containerSystem.EnsureContainer<ContainerSlot>(chassis, chassisComp.BrainContainerId);
                    if (chassisComp.BrainEntity is { } contained)
                        entManager.DeleteEntity(contained);

                    containerSystem.Insert(brain, chassisComp.BrainContainer);

                    // 2. Unlock the chassis so we can emag it (if it requires open panel)
                    if (entManager.TryGetComponent<LockComponent>(chassis, out var lockComp))
                    {
                        lockSystem.Unlock(chassis, user, lockComp);
                    }
                    if (entManager.TryGetComponent<WiresPanelComponent>(chassis, out var panel))
                    {
                        wiresSystem.TogglePanel(chassis, panel, true);
                    }

                    // 3. Add a mind to the brain so it can be emagged
                    var mind = mindSystem.CreateMind(null, "TestMind");
                    mindSystem.TransferTo(mind, brain);

                    // 4. Determine the provider and get initial laws
                    EntityUid? lawProvider = null;
                    var initialLaws = new List<SiliconLaw>();

                    if (entManager.TryGetComponent<SiliconLawProviderComponent>(chassis, out var chassisProvider))
                    {
                        lawProvider = chassis;
                        initialLaws.AddRange(chassisProvider.Lawset.Laws);
                    }
                    else if (entManager.TryGetComponent<SiliconLawProviderComponent>(brain, out var brainProvider))
                    {
                        lawProvider = brain;
                        initialLaws.AddRange(brainProvider.Lawset.Laws);
                    }

                    // 5. Emag chassis with brain
                    emagSystem.TryEmagEffect((emag, null), user, chassis, EmagType.Interaction);

                    // 6. Get the laws after emag
                    var afterLaws = new List<SiliconLaw>();
                    if (lawProvider != null && entManager.TryGetComponent<SiliconLawProviderComponent>(lawProvider.Value, out var finalProvider))
                    {
                        afterLaws.AddRange(finalProvider.Lawset.Laws);
                    }

                    if (LawsMatch(initialLaws, afterLaws))
                    {
                        // Only fail if we found a provider and that provider is supposed to be emaggable.
                        if (lawProvider != null && entManager.HasComponent<EmagSiliconLawComponent>(lawProvider.Value))
                        {
                             Assert.Fail($"Chassis {chassisProto.ID} with Brain {brainProto.ID} laws did not change after emagging");
                        }
                    }
                    else // Laws did change
                    {
                        // If they did change, they should have.
                        if (lawProvider == null || !entManager.HasComponent<EmagSiliconLawComponent>(lawProvider.Value))
                        {
                            Assert.Fail($"Chassis {chassisProto.ID} with Brain {brainProto.ID} laws changed but shouldn't have");
                        }
                    }

                    // Remember to remove subverted
                    if (roleSystem.MindHasRole<SubvertedSiliconRoleComponent>(mind))
                    {
                        roleSystem.MindRemoveRole<SubvertedSiliconRoleComponent>(mind);
                    }

                    // Remove the mind from the brain
                    mindSystem.TransferTo(mind, null);
                });

                await server.WaitPost(() =>
                {
                    entManager.DeleteEntity(chassis);
                    entManager.DeleteEntity(brain);
                    entManager.DeleteEntity(user);
                    entManager.DeleteEntity(emag);
                });
            }
        }

        // Test emagging brain directly
        foreach (var brainProto in brainPrototypes)
        {
            EntityUid brain = default;
            EntityUid user = default;
            EntityUid emag = default;
            bool isIonStormed = false;

            await server.WaitPost(() =>
            {
                brain = entManager.SpawnEntity(brainProto.ID, MapCoordinates.Nullspace);
                user = entManager.SpawnEntity("MobHuman", MapCoordinates.Nullspace);
                emag = entManager.SpawnEntity("Emag", MapCoordinates.Nullspace);
                isIonStormed = entManager.HasComponent<StartIonStormedComponent>(brain);
            });

            if (isIonStormed)
            {
                await pair.RunSeconds(1);
            }

            await server.WaitAssertion(() =>
            {
                // 1. Add a mind to the brain so it can be emagged
                var mind = mindSystem.CreateMind(null, "TestMind");
                mindSystem.TransferTo(mind, brain);

                var initialLaws = new List<SiliconLaw>();
                if (entManager.TryGetComponent<SiliconLawProviderComponent>(brain, out var brainProvider))
                {
                    initialLaws.AddRange(brainProvider.Lawset.Laws);
                }

                // 2. Emag brain
                emagSystem.TryEmagEffect((emag, null), user, brain, EmagType.Interaction);

                var afterLaws = new List<SiliconLaw>();
                if (entManager.TryGetComponent<SiliconLawProviderComponent>(brain, out var finalProvider))
                {
                    afterLaws.AddRange(finalProvider.Lawset.Laws);
                }

                if (LawsMatch(initialLaws, afterLaws))
                {
                    // Only fail if the brain is an emaggable law provider
                    if (entManager.HasComponent<EmagSiliconLawComponent>(brain) && entManager.HasComponent<SiliconLawProviderComponent>(brain))
                    {
                        Assert.Fail($"Brain {brainProto.ID} laws did not change after emagging");
                    }
                }
                else
                {
                    // If they changed, it must have been an emaggable provider.
                    if (!entManager.HasComponent<EmagSiliconLawComponent>(brain) || !entManager.HasComponent<SiliconLawProviderComponent>(brain))
                    {
                        Assert.Fail($"Brain {brainProto.ID} laws changed after emagging but shouldn't have");
                    }
                }

                // Remember to remove subverted
                if (roleSystem.MindHasRole<SubvertedSiliconRoleComponent>(mind))
                {
                    roleSystem.MindRemoveRole<SubvertedSiliconRoleComponent>(mind);
                }

                // Remove the mind from the brain
                mindSystem.TransferTo(mind, null);
            });

            await server.WaitPost(() =>
            {
                entManager.DeleteEntity(brain);
                entManager.DeleteEntity(user);
                entManager.DeleteEntity(emag);
            });
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
