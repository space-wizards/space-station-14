using Content.Shared.Dataset;
using Robust.Shared.Prototypes;

namespace Content.Server.RuntimeFun;

/// <summary>
/// Entities with this component will speak everytime an error occurs. They will say the exception
/// </summary>
[RegisterComponent]
public sealed partial class SpeakOnExceptionComponent : Component
{
    /// <summary>
    /// The last log that was spoken, used to ensure you don't repeat logs
    /// </summary>
    [DataField]
    public string? LastLog;

    /// <summary>
    /// Minimum time between error speech events.
    /// </summary>
    [DataField]
    public TimeSpan SpeechCooldown = TimeSpan.FromMinutes(1);

    /// <summary>
    /// The chance to speak without an accent.
    /// </summary>
    [DataField]
    public float ChanceSpeakNoAccent = 0.005f;

    /// <summary>
    /// Localized dataset used when speaking
    /// </summary>
    public ProtoId<LocalizedDatasetPrototype> Dataset = "ExceptionSpeechDataset";

    /// <summary>
    /// The next time the entity can say another error.
    /// </summary>
    [DataField]
    public TimeSpan? NextTimeCanSpeak;

    /// <summary>
    /// If this component is currently trying to block accents from working.
    /// </summary>
    public bool BlockAccent;
}
