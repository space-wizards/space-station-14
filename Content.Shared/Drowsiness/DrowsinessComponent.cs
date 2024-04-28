using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared.Drowsiness;

/// <summary>
///     Exists for use as a status effect. Adds a shader to the client that scales with the effect duration.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DrowsinessComponent : Component
{
    /// <summary>
    /// The random time between sleeping incidents, (min, max).
    /// </summary>
    [DataField("timeBetweenIncidents", required: true)]
    public Vector2 TimeBetweenIncidents = new Vector2(5f, 60f);

    /// <summary>
    /// The duration of sleeping incidents, (min, max).
    /// </summary>
    [DataField("durationOfIncident", required: true)]
    public Vector2 DurationOfIncident = new Vector2(10f, 30f);

    public float NextIncidentTime;
}
