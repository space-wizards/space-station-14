using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Intellicard;

/// <summary>
/// Allows this entity to download the station AI onto an AiHolderComponent.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class IntellicardComponent : Component
{
    /// <summary>
    /// The duration it takes to download the AI from an AiHolder.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int DownloadTime = 15;

    /// <summary>
    /// The duration it takes to upload the AI to an AiHolder.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int UploadTime = 3;

    /// <summary>
    /// The sound that plays for the AI
    /// when they are being downloaded
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? WarningSound = new SoundPathSpecifier("/Audio/Misc/notice2.ogg");

    /// <summary>
    /// The delay before allowing the warning to play again in seconds.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan WarningDelay = TimeSpan.FromSeconds(8);

    /// <summary>
    /// The text used for the "Wipe AI" verb on the intellicard.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId WipeVerb = "intellicard-wipe-verb";

    /// <summary>
    /// The text used for the popup after wiping the intellicard.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId WipePopup = "intellicard-wipe-popup";

    [ViewVariables]
    public TimeSpan NextWarningAllowed = TimeSpan.Zero;

    public const string Container = "station_ai_mind_slot";
}
