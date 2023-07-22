using Robust.Shared.Map.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.DayTime;
[RegisterComponent, NetworkedComponent]
public sealed class DayTimeComponent : Component
{
    [DataField("dayColor")]
    public Color DayColor = Color.White;

    [DataField("nightColor")]
    public Color NightColor = Color.Black;

    [ViewVariables(VVAccess.ReadWrite), DataField("targetColor")]
    public Color TargetColor;

    [DataField("stepTime")]
    public float StepTime = 1f;

    [ViewVariables(VVAccess.ReadWrite), DataField("currentColor")]
    public Color CurrentColor;

    public MapLightComponent? MapLightComponent;

    [ViewVariables(VVAccess.ReadWrite), DataField("time")]
    public float Time = 0.5f;
    public float Timer = 0f;

    [DataField("testColor")]
    public Vector4? color1;
}

