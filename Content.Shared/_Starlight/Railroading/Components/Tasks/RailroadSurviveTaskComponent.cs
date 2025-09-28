using Robust.Shared.Utility;

namespace Content.Shared._Starlight.Railroading;

[RegisterComponent]
public sealed partial class RailroadSurviveTaskComponent : Component
{
    [DataField]
    public string Message = "rail-survive-task";

    [DataField]
    public SpriteSpecifier Icon = new SpriteSpecifier.Rsi(new ResPath("Interface/Alerts/human_dead.rsi"), "dead");

    [DataField]
    public bool IsCompleted = true;
}
