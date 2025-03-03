using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Containers;

namespace Content.Shared.Delivery;

/// <summary>
/// Component given to deliveries.
/// Means the entity is a delivery, which upon opening will grant a reward to cargo.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DeliveryComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool IsOpened;

    [DataField, AutoNetworkedField]
    public bool IsLocked = true;

    [DataField, AutoNetworkedField]
    public int SpesoReward = 500;

    [DataField, AutoNetworkedField]
    public string? RecipientName;

    [DataField, AutoNetworkedField]
    public string? RecipientJobTitle;

    [DataField, AutoNetworkedField]
    public EntityUid? RecipientStation;

    [DataField(required: true)]
    public SoundSpecifier? UnlockSound;

    [DataField(required: true)]
    public SoundSpecifier? OpenSound;

    [DataField]
    public BaseContainer? Container = default!;
}
