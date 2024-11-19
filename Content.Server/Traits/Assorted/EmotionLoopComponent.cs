using System.Numerics;

namespace Content.Server.Traits.Assorted;

/// <summary>
/// This component allows triggering any emotion at random intervals.
/// </summary>
[RegisterComponent, Access(typeof(EmotionLoopSystem))]
public sealed partial class EmotionLoopComponent : Component
{
    /// <summary>
    /// A random interval between emotions (minimum, maximum).
    /// </summary>
    [DataField("timeBetweenEmotions", required: true)]
    public Vector2 TimeBetweenEmotions { get; private set; }

    [DataField(required: true)]
    public string Emotion { get; private set; }

    public float NextIncidentTime;
}
