using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Delivery;

/// <summary>
/// Component given to deliveries.
/// Means this delivery can be torn open using a verb.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class TearableDeliveryComponent : Component
{
    /// <summary>
    /// The amount of spesos that gets "added" to the station bank account on tear.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int SpesoPenalty = -250;

    /// <summary>
    /// The sound to play when the delivery is opened.
    /// </summary>
    [DataField]
    public SoundSpecifier? TearSound = new SoundCollectionSpecifier("DeliveryTearSounds", AudioParams.Default.WithVolume(-8));

    /// <summary>
    /// The message this delivery will speak out once it's torn open.
    /// </summary>
    [DataField]
    public LocId? TearMessage = "delivery-tearable-message";

    /// <summary>
    /// The message this delivery will speak out once it's torn open.
    /// </summary>
    [DataField]
    public LocId TearVerb = "delivery-tearable-verb";

    /// <summary>
    /// The tool quality needed to force open this delivery.
    /// </summary>
    [DataField]
    public string ToolQuality = "Slicing";

    /// <summary>
    /// Duration of the doAfter to tear the delivery open.
    /// </summary>
    [DataField]
    public TimeSpan DoAfter = TimeSpan.FromSeconds(3);
}
