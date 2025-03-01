using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Delivery;

/// <summary>
/// Component given to deliveries. I will write this eventually.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DeliveryComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool IsLocked = true;

    [DataField, AutoNetworkedField]
    public int SpesoReward = 500;

    [DataField, AutoNetworkedField]
    public string? RecipientName;

    [DataField, AutoNetworkedField]
    public string? RecipientJobTitle;

    [DataField]
    public EntityUid RecipientStation;

    [DataField(required: true)]
    public SoundSpecifier? UnlockSound;

    [DataField(required: true)]
    public SoundSpecifier? OpenSound;

    [DataField]
    public EntProtoId? Wrapper;
}
