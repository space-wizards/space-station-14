using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Utility;

namespace Content.Shared._Starlight.Railroading;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class RailroadTimerTaskComponent : Component
{
    [DataField]
    public string Message = "rail-timer-task";

    [DataField]
    public TimeSpan Duration = TimeSpan.FromMinutes(1);

    [DataField]
    public TimeSpan Started = TimeSpan.Zero;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan EndTime = TimeSpan.Zero;

    [DataField]
    public bool IsCompleted;

    [DataField]
    public SpriteSpecifier Icon = new SpriteSpecifier.Rsi(new ResPath("Objects/Devices/goldwatch.rsi"), "goldwatch");
}

