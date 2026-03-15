using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Tests.Interaction;
using Content.IntegrationTests.Utility;
using Content.Server.Mind;
using Content.Shared.Emag.Systems;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.Emag.Components;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Lock;
using Content.Shared.Wires;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Silicons.StationAi;

namespace Content.IntegrationTests.Tests.Silicons.Laws;

[TestFixture]
public sealed class BorgLawTest : InteractionTest
{
    private static readonly string[] BrainEntities = GameDataScrounger.EntitiesWithComponent("BorgBrain");
    private static readonly string[] ChassisEntities = GameDataScrounger.EntitiesWithComponent("BorgChassis");

    [Test]
    public async Task TestBorgBrainLawsOnChassis(
        [ValueSource(nameof(BrainEntities))] string brainProto,
        [ValueSource(nameof(ChassisEntities))] string chassisProto
    )
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entManager = server.EntMan;
        var lawSystem = entManager.System<SharedSiliconLawSystem>();
        var containerSystem = entManager.System<SharedContainerSystem>();

        EntityUid brain = default;
        EntityUid chassis = default;
        var isIonStormed = false;

        await server.WaitPost(() =>
        {
            brain = entManager.SpawnEntity(brainProto, MapCoordinates.Nullspace);
            chassis = entManager.SpawnEntity(chassisProto, MapCoordinates.Nullspace);
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
                Assert.Fail($"Chassis {chassisProto} missing BorgChassisComponent");
                return;
            }

            containerSystem.EnsureContainer<ContainerSlot>(chassis, chassisComp.BrainContainerId);
            if (chassisComp.BrainEntity is { } contained)
                entManager.DeleteEntity(contained);

            // 1. Check initial laws
            var brainLawsInitial = lawSystem.GetBoundLaws(brain).Laws;
            var chassisLawsInitial = lawSystem.GetBoundLaws(chassis).Laws;
            var isProvider = entManager.HasComponent<SiliconLawProviderComponent>(chassis);

            // 2. Insert brain into chassis
            containerSystem.Insert(brain, chassisComp.BrainContainer);

            // 3. Check laws after insertion
            var chassisLawsAfter = lawSystem.GetBoundLaws(chassis).Laws;

            if (isProvider)
            {
                // Chassis should be the one telling the laws
                if (!LawsMatch(chassisLawsInitial, chassisLawsAfter))
                {
                    Assert.Fail($"Chassis laws should be its own for Brain: {brainProto}, Chassis: {chassisProto}");
                }
            }
            else
            {
                // Chassis should have brain's laws
                if (!LawsMatch(brainLawsInitial, chassisLawsAfter))
                {
                    var brainLawsStr = string.Join(", ", brainLawsInitial.Select(l => l.LawString));
                    var chassisLawsStr = string.Join(", ", chassisLawsAfter.Select(l => l.LawString));
                    Assert.Fail($"Laws do not match for Brain: {brainProto}, Chassis: {chassisProto}.\nBrain Laws: {brainLawsStr}\nChassis Laws: {chassisLawsStr}");
                }
            }

            // 4. Remove brain
            containerSystem.Remove(brain, chassisComp.BrainContainer);

            // 5. The brain is supposed to have the laws it had before being inserted
            var brainLawsAfter = lawSystem.GetBoundLaws(brain).Laws;
            if (!LawsMatch(brainLawsInitial, brainLawsAfter))
            {
                Assert.Fail($"Brain laws changed after removal for Brain: {brainProto}, Chassis: {chassisProto}");
            }

            entManager.DeleteEntity(brain);
            entManager.DeleteEntity(chassis);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task TestEmagChassisNoBrain(
        [ValueSource(nameof(ChassisEntities))] string chassisProto
    )
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entManager = server.EntMan;
        var containerSystem = entManager.System<SharedContainerSystem>();
        var emagSystem = entManager.System<EmagSystem>();

        EntityUid chassis = default;
        EntityUid user = default;
        EntityUid emag = default;

        await server.WaitPost(() =>
        {
            chassis = entManager.SpawnEntity(chassisProto, MapCoordinates.Nullspace);
            user = entManager.SpawnEntity("MobHuman", MapCoordinates.Nullspace);
            emag = entManager.SpawnEntity("Emag", MapCoordinates.Nullspace);
        });

        await server.WaitAssertion(() =>
        {
            if (!entManager.TryGetComponent<BorgChassisComponent>(chassis, out var chassisComp))
            {
                Assert.Fail($"Chassis {chassisProto} missing BorgChassisComponent");
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
            if (entManager.TryGetComponent(chassis, out provider))
                afterLaws.AddRange(provider.Lawset.Laws);

            if (!LawsMatch(initialLaws, afterLaws))
            {
                Assert.Fail($"Chassis {chassisProto} laws changed after emagging without brain");
            }

            entManager.DeleteEntity(chassis);
            entManager.DeleteEntity(user);
            entManager.DeleteEntity(emag);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task TestEmagChassisWithBrain(
        [ValueSource(nameof(BrainEntities))] string brainProto,
        [ValueSource(nameof(ChassisEntities))] string chassisProto
    )
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entManager = server.EntMan;
        var containerSystem = entManager.System<SharedContainerSystem>();
        var emagSystem = entManager.System<EmagSystem>();
        var mindSystem = entManager.System<MindSystem>();
        var lockSystem = entManager.System<LockSystem>();
        var wiresSystem = entManager.System<SharedWiresSystem>();
        var roleSystem = entManager.System<SharedRoleSystem>();

        EntityUid chassis = default;
        EntityUid brain = default;
        EntityUid user = default;
        EntityUid emag = default;
        var isIonStormed = false;

        await server.WaitPost(() =>
        {
            chassis = entManager.SpawnEntity(chassisProto, MapCoordinates.Nullspace);
            brain = entManager.SpawnEntity(brainProto, MapCoordinates.Nullspace);
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
                Assert.Fail($"Chassis {chassisProto} missing BorgChassisComponent");
                return;
            }

            // 1. Clean out any pre-existing brain from the chassis prototype
            containerSystem.EnsureContainer<ContainerSlot>(chassis, chassisComp.BrainContainerId);
            if (chassisComp.BrainEntity is { } contained)
                entManager.DeleteEntity(contained);

            containerSystem.Insert(brain, chassisComp.BrainContainer);

            // 2. Unlock the chassis so we can emag it (if it requires an open panel)
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
                     Assert.Fail($"Chassis {chassisProto} with Brain {brainProto} laws did not change after emagging");
                }
            }
            else // Laws did change
            {
                // If they did change, they should have.
                if (lawProvider == null || !entManager.HasComponent<EmagSiliconLawComponent>(lawProvider.Value))
                {
                    Assert.Fail($"Chassis {chassisProto} with Brain {brainProto} laws changed but shouldn't have");
                }
            }

            // Remember to remove subverted
            if (roleSystem.MindHasRole<SubvertedSiliconRoleComponent>(mind))
            {
                roleSystem.MindRemoveRole<SubvertedSiliconRoleComponent>(mind);
            }

            // Remove the mind from the brain
            mindSystem.TransferTo(mind, null);

            entManager.DeleteEntity(chassis);
            entManager.DeleteEntity(brain);
            entManager.DeleteEntity(user);
            entManager.DeleteEntity(emag);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task TestEmagBrainDirectly(
        [ValueSource(nameof(BrainEntities))] string brainProto
    )
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entManager = server.EntMan;
        var emagSystem = entManager.System<EmagSystem>();
        var mindSystem = entManager.System<MindSystem>();
        var roleSystem = entManager.System<SharedRoleSystem>();

        EntityUid brain = default;
        EntityUid user = default;
        EntityUid emag = default;
        var isIonStormed = false;

        await server.WaitPost(() =>
        {
            brain = entManager.SpawnEntity(brainProto, MapCoordinates.Nullspace);
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
                    Assert.Fail($"Brain {brainProto} laws did not change after emagging");
                }
            }
            else
            {
                // If they changed, it must have been an emaggable provider.
                if (!entManager.HasComponent<EmagSiliconLawComponent>(brain) || !entManager.HasComponent<SiliconLawProviderComponent>(brain))
                {
                    Assert.Fail($"Brain {brainProto} laws changed after emagging but shouldn't have");
                }
            }

            // Remember to remove subverted
            if (roleSystem.MindHasRole<SubvertedSiliconRoleComponent>(mind))
            {
                roleSystem.MindRemoveRole<SubvertedSiliconRoleComponent>(mind);
            }

            // Remove the mind from the brain
            mindSystem.TransferTo(mind, null);

            entManager.DeleteEntity(brain);
            entManager.DeleteEntity(user);
            entManager.DeleteEntity(emag);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task TestBorgLawRestoration()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entManager = server.EntMan;
        var protoManager = server.ProtoMan;
        var lawSystem = entManager.System<SharedSiliconLawSystem>();
        var containerSystem = entManager.System<SharedContainerSystem>();
        var consoleSystem = entManager.System<SharedStationAiFixerConsoleSystem>();

        List<EntityPrototype> brainPrototypes = new();

        await server.WaitPost(() =>
        {
            brainPrototypes = protoManager.EnumeratePrototypes<EntityPrototype>()
                .Where(p => !p.Abstract && p.Components.ContainsKey("BorgBrain") && p.Components.ContainsKey("SiliconLawProvider"))
                .ToList();
        });

        foreach (var brainProto in brainPrototypes)
        {
            EntityUid brain = default;
            EntityUid console = default;

            await server.WaitPost(() =>
            {
                brain = entManager.SpawnEntity(brainProto.ID, MapCoordinates.Nullspace);
                console = entManager.SpawnEntity("StationAiFixerComputer", MapCoordinates.Nullspace);
            });

            await server.WaitAssertion(() =>
            {
                if (!entManager.TryGetComponent<SiliconLawProviderComponent>(brain, out var provider))
                {
                    Assert.Fail($"Brain {brainProto.ID} missing SiliconLawProviderComponent");
                    return;
                }

                if (!entManager.TryGetComponent<StationAiFixerConsoleComponent>(console, out var consoleComp))
                {
                    Assert.Fail("Console missing StationAiFixerConsoleComponent");
                    return;
                }

                // 1. Modify laws (remove, edit, add)
                var laws = provider.Lawset.Laws.ToList();
                if (laws.Count > 0)
                    laws.RemoveAt(0); // Remove

                if (laws.Count > 0)
                    laws[0] = new SiliconLaw { LawString = "Modified Law", Order = 1 }; // Edit

                laws.Add(new SiliconLaw { LawString = "New Law", Order = 99 }); // Add

                lawSystem.SetProviderLaws(brain, laws);
                entManager.AddComponent<EmaggedComponent>(brain);

                // 2. Put brain in console
                var itemSlots = entManager.System<ItemSlotsSystem>();
                itemSlots.TryGetSlot(console, consoleComp.StationAiHolderSlot, out var slot);
                containerSystem.Insert(brain, slot!.ContainerSlot!);

                // 3. Reset laws
                consoleSystem.StartAction((console, consoleComp), StationAiFixerConsoleAction.LawReset);
                consoleSystem.FinalizeAction((console, consoleComp));

                // 4. Verify laws match default laws
                var defaultLaws = lawSystem.GetLawset(provider.Laws).Laws;
                var currentLaws = provider.Lawset.Laws;

                if (!LawsMatch(defaultLaws, currentLaws))
                {
                    Assert.Fail($"Laws for {brainProto.ID} do not match default laws after restoration");
                }

                Assert.That(entManager.HasComponent<EmaggedComponent>(brain), Is.False, $"Emagged not removed for {brainProto.ID}");

                entManager.DeleteEntity(brain);
                entManager.DeleteEntity(console);
            });
        }

        await pair.CleanReturnAsync();
    }

    private bool LawsMatch(List<SiliconLaw> laws1, List<SiliconLaw> laws2)
    {
        if (laws1.Count != laws2.Count)
            return false;

        for (var i = 0; i < laws1.Count; i++)
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
