using System.Numerics;

namespace Content.Server.Traits.Assorted;

/// <summary>
/// This component allows triggering any emotion at random intervals.
/// </summary>
[RegisterComponent, Access(typeof(EmotionLoopSystem))]
public sealed partial class EmotionLoopComponent : Component
{
    /// <summary>
    /// A minimum interval between emotions.
    /// </summary>
    [DataField("minTimeBetweenEmotions", required: true)]
    public float MinTimeBetweenEmotions { get; private set; }

    /// <summary>
    /// A maximum interval between emotions.
    /// </summary>
    [DataField("maxTimeBetweenEmotions", required: true)]
    public float MaxTimeBetweenEmotions { get; private set; }

    /// <summary>
    /// A list of available emotions for playback.
    /// </summary>
    [DataField]
    public List<string> Emotions = new();

    /// <summary>
    /// The index of the next emotion from the "Emotions" list.
    /// </summary>
    public int NextEmotionIndex = 0;

    public TimeSpan NextIncidentTime;
}
