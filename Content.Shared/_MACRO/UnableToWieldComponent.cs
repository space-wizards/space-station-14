using Robust.Shared.GameStates;

namespace Content.Shared._MACRO.UnableToWield;

[RegisterComponent, NetworkedComponent]
public sealed partial class UnableToWieldComponent : Component
{
    [DataField]
    public LocId? PopupText = "unable-to-wield-cant-do";
}
