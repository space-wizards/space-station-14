using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Silicons.StationAi;

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
    public int downloadTime = 15;

    /// <summary>
    /// The duration it takes to upload the AI to an AiHolder.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int uploadTime = 3;

    /// <summary>
    /// The sound that plays for the Silicon player
    /// when the law change is processed for the provider.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? WarningSound = new SoundPathSpecifier("/Audio/Misc/notice2.ogg");

    /// <summary>
    /// The delay before allowing the warning to play again in seconds.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan WarningDelay = TimeSpan.FromSeconds(8);

    public TimeSpan NextWarningAllowed = TimeSpan.Zero;

    public const string HolderContainer = "station_ai_mind_slot";
}
