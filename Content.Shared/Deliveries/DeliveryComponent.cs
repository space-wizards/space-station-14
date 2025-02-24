using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Deliveries;

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
    public string RecipientName = string.Empty;

    [DataField, AutoNetworkedField]
    public string RecipientJobTitle = string.Empty;

    [DataField]
    public EntityUid RecipientStation;

    [DataField(required: true)]
    public SoundSpecifier? OpenSound;

    [DataField]
    public EntProtoId? Wrapper = "PresentTrash";
}
