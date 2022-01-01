using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Species;

[Prototype("species")]
public class SpeciesPrototype : IPrototype
{
    [DataField("id", required: true)]
    public string ID { get; } = default!;

    [DataField("name", required: true)]
    public string Name { get; } = default!;

    [DataField("prototype", required: true)]
    public string Prototype { get; } = default!;

    [DataField("dollPrototype", required: true)]
    public string DollPrototype { get; } = default!;

    [DataField("skinColoration", required: true)]
    public SpeciesSkinColor SkinColoration { get; }
}

public enum SpeciesSkinColor
{
    HumanToned,
    Hues,
}
