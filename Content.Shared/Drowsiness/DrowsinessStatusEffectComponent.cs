using System.Numerics;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Drowsiness;

/// <summary>
/// Exists for use as a status effect. Adds a shader to the client that scales with the effect duration.
/// Use only in conjunction with <see cref="StatusEffectComponent"/>, on the status effect entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class DrowsinessStatusEffectComponent : Component
{
    /// <summary>
    /// The random time between sleeping incidents, (min, max).
    /// </summary>
    [DataField]
    public Vector2 TimeBetweenIncidents = new(5f, 60f);

    /// <summary>
    /// The duration of sleeping incidents, (min, max).
    /// </summary>
    [DataField]
    public Vector2 DurationOfIncident = new(2, 5);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextIncidentTime = TimeSpan.Zero;
}
