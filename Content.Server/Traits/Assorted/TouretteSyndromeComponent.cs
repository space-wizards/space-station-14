namespace Content.Server.Traits.Assorted;


/// <summary>
/// This is used for the occasional scream/speak.
/// </summary>
[RegisterComponent]
public sealed class TouretteSyndromeComponent : Component
{
    /// <summary>
    /// The random time between incidents, (min, max).
    /// </summary>
    [DataField("timeBetweenIncidents", required: true)]
    public Vector2 TimeBetweenIncidents { get; }

    public float NextIncidentTime;

 }

