using Robust.Shared.Audio;

namespace Content.Server.Traits.Assorted;

/// <summary>
/// This is used for the occasional sneeze or cough.
/// </summary>
[RegisterComponent]
public sealed class UncontrollableSnoughComponent : Component
{
    /// <summary>
    /// Message to play when snoughing.
    /// </summary>
    [DataField("snoughMessage")] public string SnoughMessage = "disease-sneeze";

    /// <summary>
    /// Sound to play when snoughing.
    /// </summary>
    [DataField("snoughSound")] public SoundSpecifier? SnoughSound;

    /// <summary>
    /// The random time between incidents, (min, max).
    /// </summary>
    [DataField("timeBetweenIncidents", required: true)]
    public Vector2 TimeBetweenIncidents { get; }

    public float NextIncidentTime;
}
