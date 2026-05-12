using Robust.Shared.GameStates;

namespace Content.Shared._Tinystation.Knight.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class KnightLayOnHandsComponent : Component
{
    [DataField]
    public float HealAmount = 30f;

    [DataField]
    public string HealPopup = "knight-lay-on-hands-popup";

    [DataField]
    public string HealPopupOthers = "knight-lay-on-hands-popup-others";

    [DataField]
    public string FailPopup = "knight-lay-on-hands-fail";
}
