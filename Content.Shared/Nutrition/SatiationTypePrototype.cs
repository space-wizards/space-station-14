using Robust.Shared.Prototypes;

namespace Content.Shared.Nutrition;

[Prototype]
public sealed partial class SatiationTypePrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// A player-facing name used to describe this satiation.
    /// </summary>
    [DataField]
    public readonly LocId Name;
}
