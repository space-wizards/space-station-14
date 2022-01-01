using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Species;

[Prototype("species")]
public class SpeciesPrototype : IPrototype
{
    [ViewVariables]
    [DataField("id", required: true)]
    public string ID { get; } = default!;

    [ViewVariables]
    [DataField("name", required: true)]
    public string Name { get; } = default!;

    [ViewVariables]
    [DataField("prototype", required: true)]
    public string Prototype { get; } = default!;

    [ViewVariables]
    [DataField("dollPrototype", required: true)]
    public string DollPrototype { get; } = default!;
}
