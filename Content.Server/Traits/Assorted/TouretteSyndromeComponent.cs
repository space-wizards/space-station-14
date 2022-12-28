using System.ComponentModel.DataAnnotations;

namespace Content.Server.Traits.Assorted;


/// <summary>
/// This is used for the occasional scream/speak.
/// </summary>
[RegisterComponent]
public sealed class TouretteSyndromeComponent : Component
{
    /// <summary>
    /// Message to play when using item in active hand.
    /// </summary>
    [DataField("wristSpasmMessage", required: true)]
    public string TouretteUseItemMessage { get; } = default!;

    /// <summary>
    /// Message to play when drops item from active hand.
    /// </summary>
    [DataField("armTwitchingMessage", required: true)]
    public string TouretteDropItemMessage { get; } = default!;

    /// <summary>
    /// The random time between incidents, (min, max).
    /// </summary>
    [DataField("timeBetweenIncidents", required: true)]
    public Vector2 TimeBetweenIncidents { get; }

    /// <summary>
    /// The random number to choose symptom, (first, last).
    /// </summary>
    [DataField("touretteSymptoms", required: true)]
    public List<string> TouretteSymptoms { get; } = default!;

    public float NextIncidentTime;

 }

