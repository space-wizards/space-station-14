using Robust.Shared.GameStates;

namespace Content.Shared.Clothing.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class PoorlyAttachedComponent : Component
{
    [DataField, AutoNetworkedField]
    [AutoPausedField]
    public TimeSpan AttachmentTime;

    [DataField, AutoNetworkedField]
    public float EventStrengthTotal;

    [DataField]
    public TimeSpan PassiveDetachDuration = TimeSpan.FromMinutes(20);

    public float LossPerSecond => 1f / (float)PassiveDetachDuration.TotalSeconds;

    [DataField]
    public float LossPerFall = 0.2f;

    [DataField]
    public bool OthersCanReattach = true;

    [DataField]
    public LocId DetachPopup = "poorly-attached-detach-popup";

    [DataField]
    public LocId ReattachVerb = "poorly-attached-reattach-verb-default";

    [DataField]
    public LocId ReattachSelfPopupUser = "poorly-attached-reattach-self-popup-user";

    [DataField]
    public LocId ReattachSelfPopupOthers = "poorly-attached-reattach-self-popup-others";

    [DataField]
    public LocId ReattachOtherPopupUser = "poorly-attached-reattach-other-popup-user";

    [DataField]
    public LocId ReattachOtherPopupWearer = "poorly-attached-reattach-other-popup-wearer";

    [DataField]
    public LocId ReattachOtherPopupOthers = "poorly-attached-reattach-other-popup-others";
}
