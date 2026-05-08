using Robust.Shared.GameStates;

namespace Content.Shared.Shadowkin;

[RegisterComponent, NetworkedComponent]
public sealed partial class ShadowkinAnchorComponent : Component
{
    [DataField]
    public EntityUid? AnchorOwner;
}
