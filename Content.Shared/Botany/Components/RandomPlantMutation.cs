using Content.Shared.EntityEffects;
using Robust.Shared.Serialization;

namespace Content.Shared.Botany.Components;

/// <summary>
/// Data that specifies the odds and effects of possible random plant mutations.
/// A mutation is a one-time roll; to give a plant a lasting, heritable change,
/// use an effect that adds a <see cref="Traits.PlantTraitPrototype"/>.
/// </summary>
[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class RandomPlantMutation
{
    /// <summary>
    /// Odds of this mutation occurring with 1 point of mutation severity on a plant.
    /// </summary>
    [DataField]
    public float BaseOdds;

    /// <summary>
    /// The name of this mutation.
    /// </summary>
    [DataField(required: true)]
    public string Name;

    /// <summary>
    /// The EntityEffect applied to the plant when this mutation occurs.
    /// </summary>
    [DataField(required: true)]
    public EntityEffect Effect;
}
