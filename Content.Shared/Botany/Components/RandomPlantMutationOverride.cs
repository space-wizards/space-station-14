using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Botany.Components;

/// <summary>
/// A plant mutation prototype reference and optional overrides for its default values.
/// </summary>
[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class RandomPlantMutationOverride
{
    /// <summary>
    /// The prototype supplying defaults for fields not overridden by this entry.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<RandomPlantMutationPrototype> Prototype;

    /// <summary>
    /// Overrides the prototype's default value for Name.
    /// </summary>
    [DataField]
    public string? Name;

    /// <summary>
    /// Overrides the prototype's default value for Description.
    /// </summary>
    [DataField]
    public LocId? Description;

    /// <summary>
    /// Overrides the prototype's default value for Effect.
    /// </summary>
    [DataField]
    public EntityEffect? Effect;
}
