using System.ComponentModel.DataAnnotations;

namespace Content.Server.Traits.Assorted;


/// <summary>
/// This is used for the occasional tourette syndrome trait.
/// </summary>
[RegisterComponent]
public sealed class TouretteSyndromeComponent : Component
{
    /// <summary>
    /// Tourette phrases list which spoken by character.
    /// </summary>
    [DataField("tourettePhrases", required: true)]
    public List<string> TourettePhrases { get; } = default!;

    /// <summary>
    /// The random time between incidents, (min, max).
    /// </summary>
    [DataField("timeBetweenIncidents", required: true)]
    public Vector2 TimeBetweenIncidents { get; }

    /// <summary>
    /// The duration of jittering effect, (min, max).
    /// </summary>
    [DataField("jitteringDuration", required: true)]
    public Vector2 TouretteJitteringDuration { get; }

    /// <summary>
    /// The random number to choose symptom, (first, last).
    /// </summary>
    [DataField("touretteSymptoms", required: true)]
    public List<string> TouretteSymptoms { get; } = default!;

    public float NextIncidentTime;

 }

