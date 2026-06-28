using Robust.Shared.GameStates;

namespace Content.Shared.Speech.Muting;

/// <summary>
/// Marks a status effect that prevents speaking, screaming, and vocal emotes.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MutedStatusEffectComponent : Component
{
    /// <summary>
    /// Popup shown when speech is blocked.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId SpeakPopup = "speech-muted";

    /// <summary>
    /// Popup shown when screaming is blocked.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId ScreamPopup = "speech-muted";
}
