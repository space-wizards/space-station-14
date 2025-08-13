namespace Content.Shared.Radio.Components;

/// <summary>
///     This component is used to tag players that are currently wearing an ACTIVE headset.
/// </summary>
[RegisterComponent]
public sealed partial class WearingHeadsetComponent : Component
{
    [DataField]
    public EntityUid Headset;
}
