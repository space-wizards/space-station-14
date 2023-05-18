namespace Content.Server._FTL.AmbientHeater;

[RegisterComponent]
public sealed class AmbientHeaterComponent : Component
{
    [DataField("targetTemperature")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float TargetTemperature = 293.15f;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("heatPerSecond")]
    public float HeatPerSecond = 100f;

    [ViewVariables(VVAccess.ReadOnly)] [DataField("requirePower")]
    public bool RequiresPower = false;

    [ViewVariables(VVAccess.ReadOnly)] public bool Powered = false;
}
