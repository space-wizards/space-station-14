using Robust.Shared.Prototypes;

namespace Content.Shared.Chemistry.Reaction.Prototypes;

/// <summary>
/// Acts as a group for absorptions, this is mainly for ease of use so that you can just add a group instead of having
/// to add all of it's reagents individual to the ChemicalAbsorberComponent
/// </summary>
[Prototype]
public sealed partial class AbsorptionGroupPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    /// List of absorptions that are part of this absorption group
    /// Absorptions can be part of multiple absorption groups
    /// </summary>
    [DataField(required:true)]
    public HashSet<ProtoId<AbsorptionPrototype>> Absorptions = new();
}
