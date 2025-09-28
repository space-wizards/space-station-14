using Robust.Shared.Utility;

namespace Content.Shared._Starlight.Railroading;

[RegisterComponent]
public sealed partial class RailroadAvoidHandcuffsTaskComponent : Component
{
    [DataField]
    public string Message = "rail-avoid-handcuffs-task";

    [DataField]
    public SpriteSpecifier Icon = new SpriteSpecifier.Rsi(new ResPath("Objects/Misc/handcuffs.rsi"), "handcuff");

    [DataField]
    public bool IsCompleted = true;
}