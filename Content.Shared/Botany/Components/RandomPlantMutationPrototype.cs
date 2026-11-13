using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared.Botany.Components;

/// <summary>
/// Prototype that defines a plant mutation and its default values.
/// </summary>
[Prototype]
public sealed partial class RandomPlantMutationPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The name of this mutation.
    /// </summary>
    [DataField(required: true)]
    public string Name;

    /// <summary>
    /// The text to display to players when examining something with this mutation.
    /// </summary>
    [DataField]
    public LocId? Description;

    /// <summary>
    /// The effect to apply to the plant when this mutation triggers.
    /// </summary>
    [DataField(required: true)]
    public EntityEffect Effect;
}
