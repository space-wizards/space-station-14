#nullable enable
using Content.IntegrationTests.Tests.Interaction;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Construction.Interaction;

public sealed class MachineConstruction : InteractionTest
{
    private static readonly EntProtoId MachineFrame = "MachineFrame";
    private static readonly EntProtoId Unfinished = "UnfinishedMachineFrame";
    private static readonly EntProtoId ProtolatheBoard = "ProtolatheMachineCircuitboard";
    private static readonly EntProtoId Protolathe = "Protolathe";
    private static readonly EntProtoId Beaker = "Beaker";
    private static readonly EntProtoId AutolatheBoard = "AutolatheMachineCircuitboard";
    private static readonly EntProtoId Autolathe = "Autolathe";

    [Test]
    public async Task ConstructProtolathe()
    {
        await StartConstruction(MachineFrame);
        await InteractUsing(Steel, 5);
        ClientAssertPrototype(Unfinished, Target);
        await Interact(Wrench, Cable);
        AssertPrototype(MachineFrame);
        await Interact(ProtolatheBoard, Manipulator1, Manipulator1, Manipulator1, Manipulator1, Beaker, Beaker, Screw);
        AssertPrototype(Protolathe);
    }

    [Test]
    public async Task DeconstructProtolathe()
    {
        await StartDeconstruction(Protolathe);
        await Interact(Screw, Pry);
        AssertPrototype(MachineFrame);
        await Interact(Pry, Cut);
        AssertPrototype(Unfinished);
        await Interact(Wrench, Screw);
        AssertDeleted();
        await AssertEntityLookup(
            (Steel, 5),
            (Cable, 1),
            (Beaker, 2),
            (Manipulator1, 4),
            (ProtolatheBoard, 1));
    }

    [Test]
    public async Task ChangeMachine()
    {
        // Partially deconstruct a protolathe.
        await SpawnTarget(Protolathe);
        await Interact(Screw, Pry, Pry);
        AssertPrototype(MachineFrame);

        // Change it into an autolathe
        await InteractUsing(AutolatheBoard);
        AssertPrototype(MachineFrame);
        await Interact(Manipulator1, Manipulator1, Manipulator1, Manipulator1, Glass, Screw);
        AssertPrototype(Autolathe);
    }
}

