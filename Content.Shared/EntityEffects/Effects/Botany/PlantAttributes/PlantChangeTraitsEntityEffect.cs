

namespace Content.Shared.EntityEffects.Effects.Botany;

/// <summary>
/// See server-side system.
/// </summary>
public sealed partial class PlantChangeTraits : EntityEffectBase<PlantChangeTraits>
{
    /// <summary>
    /// Name of a <see cref="PlantTraitsSystem.PlantTrait"/> type.
    /// </summary>
    [DataField(required: true)]
    public string Trait;

    /// <summary>
    /// If true, the trait is removed. If false, the trait is added.
    /// </summary>
    [DataField]
    public bool Remove;
}
