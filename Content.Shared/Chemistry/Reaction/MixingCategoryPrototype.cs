using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

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

    /// <summary>
    /// A locale string used in the guidebook to describe this mixing category.
    /// </summary>
    [DataField(required: true)]
    public LocId VerbText;

    /// <summary>
    /// An icon used to represent this mixing category in the guidebook.
    /// </summary>
    [DataField(required: true)]
    public SpriteSpecifier Icon = default!;
}
