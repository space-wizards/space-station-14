using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Delivery;

/// <summary>
/// Component given to deliveries.
/// Means the entity is a delivery, which upon opening will grant a reward to cargo.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class DeliveryComponent : Component
{
    /// <summary>
    /// Whether this delivery has been opened before.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsOpened;

    /// <summary>
    /// Whether this delivery is still locked using the fingerprint reader.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsLocked = true;

    /// <summary>
    /// The base amount of spesos that gets added to the station bank account on unlock.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int BaseSpesoReward = 500;

    /// <summary>
    /// The base amount of spesos that will be removed from the station bank account on a penalized delivery
    /// </summary>
    [DataField, AutoNetworkedField]
    public int BaseSpesoPenalty = 250;

    /// <summary>
    /// The name of the recipient of this delivery.
    /// Used for the examine text.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? RecipientName;

    /// <summary>
    /// The job of the recipient of this delivery.
    /// Used for the examine text.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? RecipientJobTitle;

    /// <summary>
    /// The EntityUid of the station this delivery was spawned on.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? RecipientStation;

    /// <summary>
    /// The bank account ID of the account to subtract funds from in case of penalization
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<CargoAccountPrototype> PenaltyBankAccount = "Cargo";

    /// <summary>
    /// Whether this delivery has already received a penalty.
    /// Used to avoid getting penalized several times.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool WasPenalized;

    /// <summary>
    /// The sound to play when the delivery is unlocked.
    /// </summary>
    [DataField]
    public SoundSpecifier? UnlockSound = new SoundCollectionSpecifier("DeliveryUnlockSounds", AudioParams.Default.WithVolume(-10));

    /// <summary>
    /// The sound to play when the delivery is opened.
    /// </summary>
    [DataField]
    public SoundSpecifier? OpenSound = new SoundCollectionSpecifier("DeliveryOpenSounds");

    /// <summary>
    /// The container with all the contents of the delivery.
    /// </summary>
    [DataField]
    public string Container = "delivery";
}
