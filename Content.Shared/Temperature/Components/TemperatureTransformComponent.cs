using System.Numerics;
using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.GameStates;

namespace Content.Shared.Temperature.Components;

/// <summary>
/// Replaces the entity when a certain temperature range is met.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class TemperatureTransformComponent : Component
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
    /// What to replace the entity with.
    /// </summary>
    [DataField(required:true)]
    public EntityTableSelector Table;

    /// <summary>
    /// The range at which the entity will be replaced, where X is the min temperature and Y is the max, zero is considered unlimited.
    /// </summary>
    [DataField(required:true)]
    public Vector2 TemperatureRange;
}
