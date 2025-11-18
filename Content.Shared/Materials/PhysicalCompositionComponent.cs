using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Shared.Materials;

/// <summary>
/// This is used for assigning an innate material/chemical composition to an entity.
/// These aren't materials per se, but rather the materials which "make up" an entity.
/// This also isn't something that should exist simultaneously with <see cref="MaterialComponent"/>.
/// </summary>
/// <remarks>
/// The reason for duel material/chemical is for the eventual
/// combination of the two systems.
/// </remarks>
[RegisterComponent]
public sealed partial class PhysicalCompositionComponent : Component
{
    /// <summary>
    /// The materials that "make up" this entity
    /// </summary>
    [DataField("materialComposition", customTypeSerializer: typeof(PrototypeIdDictionarySerializer<int, MaterialPrototype>))]
    public Dictionary<string, int> MaterialComposition = new();

    /// <summary>
    /// The chemicals that "make up" this entity
    /// </summary>
    [DataField("chemicalComposition", customTypeSerializer: typeof(PrototypeIdDictionarySerializer<FixedPoint2, ReagentPrototype>))]
    public Dictionary<string, FixedPoint2> ChemicalComposition = new();
    // TODO use ReagentQuantity[]
}
