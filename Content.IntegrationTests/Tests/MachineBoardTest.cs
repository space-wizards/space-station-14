using System.Collections.Generic;
using System.Linq;
using Content.Server.Construction.Components;
using Content.Shared.Construction.Components;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests;

public sealed class MachineBoardTest
{
    /// <summary>
    /// A list of machine boards that can be ignored by this test.
    /// </summary>
    private readonly HashSet<string> _ignoredPrototypes = new()
    {
        //These have their own construction thing going on here
        "MachineParticleAcceleratorEndCapCircuitboard",
        "MachineParticleAcceleratorFuelChamberCircuitboard",
        "MachineParticleAcceleratorFuelChamberCircuitboard",
        "MachineParticleAcceleratorPowerBoxCircuitboard",
        "MachineParticleAcceleratorEmitterStarboardCircuitboard",
        "MachineParticleAcceleratorEmitterForeCircuitboard",
        "MachineParticleAcceleratorEmitterPortCircuitboard",
        "ParticleAcceleratorComputerCircuitboard"
    };

    /// <summary>
    /// Ensures that every single machine board's corresponding entity
    /// is a machine and can be properly deconstructed.
    /// </summary>
    [Test]
    public async Task TestMachineBoardHasValidMachine()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var protoMan = server.ResolveDependency<IPrototypeManager>();

        await server.WaitAssertion(() =>
        {
            foreach (var p in protoMan.EnumeratePrototypes<EntityPrototype>()
                         .Where(p => !p.Abstract)
                         .Where(p => !pair.IsTestPrototype(p))
                         .Where(p => !_ignoredPrototypes.Contains(p.ID)))
            {
                if (!p.TryGetComponent<MachineBoardComponent>(out var mbc))
                    continue;
                var mId = mbc.Prototype;

                Assert.Multiple(() =>
                {
                    Assert.That(mId, Is.Not.Null, $"Machine board {p.ID} does not have a corresponding machine.");
                    Assert.That(protoMan.TryIndex<EntityPrototype>(mId, out var mProto),
                        $"Machine board {p.ID}'s corresponding machine has an invalid prototype.");
                    Assert.That(mProto.TryGetComponent<MachineComponent>(out var mComp),
                        $"Machine board {p.ID}'s corresponding machine {mId} does not have MachineComponent");
                    Assert.That(mComp.BoardPrototype, Is.EqualTo(p.ID),
                        $"Machine {mId}'s BoardPrototype is not equal to it's corresponding machine board, {p.ID}");
                });
            }
        });

        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// Ensures that every single computer board's corresponding entity
    /// is a computer that can be properly deconstructed to the correct board
    /// </summary>
    [Test]
    public async Task TestComputerBoardHasValidComputer()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var protoMan = server.ResolveDependency<IPrototypeManager>();

        await server.WaitAssertion(() =>
        {
            foreach (var p in protoMan.EnumeratePrototypes<EntityPrototype>()
                         .Where(p => !p.Abstract)
                         .Where(p => !pair.IsTestPrototype(p))
                         .Where(p => !_ignoredPrototypes.Contains(p.ID)))
            {
                if (!p.TryGetComponent<ComputerBoardComponent>(out var cbc))
                    continue;
                var cId = cbc.Prototype;

                Assert.Multiple(() =>
                {
                    Assert.That(cId, Is.Not.Null, $"Computer board \"{p.ID}\" does not have a corresponding computer.");
                    Assert.That(protoMan.TryIndex<EntityPrototype>(cId, out var cProto),
                        $"Computer board \"{p.ID}\"'s corresponding computer has an invalid prototype.");
                    Assert.That(cProto.TryGetComponent<ComputerComponent>(out var cComp),
                        $"Computer board {p.ID}'s corresponding computer \"{cId}\" does not have ComputerComponent");
                    Assert.That(cComp.BoardPrototype, Is.EqualTo(p.ID),
                        $"Computer \"{cId}\"'s BoardPrototype is not equal to it's corresponding computer board, \"{p.ID}\"");
                });
            }
        });

        await pair.CleanReturnAsync();
    }
}
