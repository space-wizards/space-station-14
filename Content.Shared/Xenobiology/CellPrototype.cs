using Robust.Shared.Prototypes;

namespace Content.Shared.Xenobiology;

[Prototype]
public sealed class CellPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = string.Empty;

    [DataField]
    public float Stability = 1;

    [DataField]
    public List<CellModifier> Modifiers = [];
}
