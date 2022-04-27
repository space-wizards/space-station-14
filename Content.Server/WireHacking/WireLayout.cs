using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Server.Wires;

// WireLayout prototype.
//
// This is meant for ease of organizing wire sets on entities that use
// wires. Once one of these is initialized, it should be stored in the
// WiresSystem as a functional wire set.
[Prototype("wireLayout")]
public sealed class WireLayoutPrototype : IPrototype
{
    [IdDataFieldAttribute]
    public string ID { get; } = default!;

    [DataField("parent")]
    public string Parent { get; } = default!;

    [DataField("dummyWires")]
    public int DummyWires { get; } = default!;

    // TODO: Repeat wires of the same action type...
    // This might sound niche, but is useful for when
    // you want wire redundancy without having to
    // define new wire actions/create trees of inheritance.
    //
    // (see: doors w/ 2 power wires)
    [DataField("wires")]
    public List<IWireAction>? Wires { get; }
}
