using System.Numerics;
using Content.Shared.EntityEffects;
using Robust.Shared.GameStates;

namespace Content.Shared.Temperature.Components;

/// <summary>
/// Replaces the entity when a certain temperature range is met.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class EntityEffectOnTemperatureComponent : Component
{
    /// <summary>
    /// A list of <see cref="TemperatureTransformEntry"/>.
    /// </summary>
    [DataField(required: true)]
    public List<TemperatureTransformEntry> Entries = new();
}

[DataRecord]
public partial record struct TemperatureTransformEntry
{
    /// <summary>
    /// List of entity effects to apply to the entity.
    /// </summary>
    [DataField(required: true)]
    public EntityEffect[] Effects;

    /// <summary>
    /// The scale of the entity effect.
    /// </summary>
    [DataField]
    public float Scale = 1f;

    /// <summary>
    /// The range at which the entity will be replaced, where X is the min temperature and Y is the max.
    /// </summary>
    [DataField(required:true)]
    public Vector2 TemperatureRange;
}
