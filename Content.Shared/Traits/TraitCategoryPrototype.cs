using Robust.Shared.Prototypes;

namespace Content.Shared.Traits;

/// <summary>
///     Describes a trait.
/// </summary>
[Prototype("traitCategory")]
public sealed partial class TraitCategoryPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///     Name of the trait category displayed in the UI
    /// </summary>
    [DataField]
    public LocId Name { get; private set; } = "";

    /// <summary>
    ///     The maximum number of traits that can be taken in this category. If -1, you can take as many traits as you like.
    /// </summary>
    [DataField]
    public int MaxTraits = -1;

    /// <summary>
    ///     All the traits in this group.
    /// </summary>
    [DataField]
    public List<ProtoId<TraitPrototype>> Traits = new();
}
