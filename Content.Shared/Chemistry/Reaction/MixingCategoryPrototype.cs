using Robust.Shared.Prototypes;

namespace Content.Shared.Chemistry.Reaction;

/// <summary>
/// This is a prototype for a method of chemical mixing, to be used by <see cref="ReactionMixerComponent"/>
/// </summary>
[Prototype("mixingCategory")]
public sealed partial class MixingCategoryPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;
}
