namespace Content.Server.Traits.Assorted;

/// <summary>
/// This is used for the narcolepsy trait.
/// </summary>
[RegisterComponent]
public sealed class NarcolepsyComponent : Component
{
    [DataField("timeBetweenIncidents", required: true)]
    public Vector2 TimeBetweenIncidents { get; } = default!;
    [DataField("durationOfIncident", required: true)]
    public Vector2 DurationOfIncident { get; }  = default!;

    public float NextIncidentTime;
}
