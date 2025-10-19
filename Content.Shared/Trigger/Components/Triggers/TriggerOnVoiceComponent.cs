using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Sends a trigger when the keyphrase is heard.
/// The User is the speaker.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnVoiceComponent : BaseTriggerOnXComponent
{
    /// <summary>
    /// Whether or not the component is actively listening at the moment.
    /// </summary>
    [ViewVariables]
    public bool IsListening => IsRecording || !string.IsNullOrWhiteSpace(KeyPhrase);

    /// <summary>
    /// The keyphrase that has been set to trigger it.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? KeyPhrase;

    /// <summary>
    /// The default keyphrase that is used when the trigger's keyphrase is reset.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId? DefaultKeyPhrase;

    /// <summary>
    /// Range in which we listen for the keyphrase.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int ListenRange = 4;

    /// <summary>
    /// Whether we are currently recording a new keyphrase.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsRecording;

    /// <summary>
    /// Minimum keyphrase length.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MinLength = 3;

    /// <summary>
    /// Maximum keyphrase length.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MaxLength = 50;

    /// <summary>
    /// When examining the item, should it show information about what word is recorded?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ShowExamine = true;

    /// <summary>
    /// Should there be verbs that allow re-recording of the trigger word?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ShowVerbs = true;

    /// <summary>
    /// The verb text that is shown when you can start recording a message.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId StartRecordingVerb = "trigger-on-voice-record";

    /// <summary>
    /// The verb text that is shown when you can stop recording a message.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId StopRecordingVerb = "trigger-on-voice-stop";

    /// <summary>
    /// Tooltip that appears when hovering over the stop or start recording verbs.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId? RecordingVerbMessage;

    /// <summary>
    /// The verb text that is shown when you can reset keyphrase to default.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId ResetRecordingVerb = "trigger-on-voice-default";

    /// <summary>
    /// The verb text that is shown when you can clear a recording.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId ClearRecordingVerb = "trigger-on-voice-clear";

    /// <summary>
    /// The loc string that is shown when inspecting an uninitialized voice trigger.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId? InspectUninitializedLoc = "trigger-on-voice-uninitialized";

    /// <summary>
    /// The loc string to use when inspecting voice trigger. Will also include the triggering phrase
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId? InspectInitializedLoc = "trigger-on-voice-examine";
}
