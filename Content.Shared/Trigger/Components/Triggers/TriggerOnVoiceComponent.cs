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
}
