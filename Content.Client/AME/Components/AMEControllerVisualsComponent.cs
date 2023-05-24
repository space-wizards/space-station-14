namespace Content.Client.AME.Components;

[RegisterComponent]
public sealed class AmeControllerVisualsComponent : Component
{
    /// <summary>
    /// The RSI state used for the AME controller display when the AME is on.
    /// </summary>
    [DataField("stateOn")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string StateOn = "control_on";

    /// <summary>
    /// The RSI state used for the AME controller display when the AME is overloading.
    /// </summary>
    [DataField("stateCritical")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string StateCritical = "control_critical";

    /// <summary>
    /// The RSI state used for the AME controller display when the AME is about to explode.
    /// </summary>
    [DataField("stateFuck")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string StateFuck = "control_fuck";
}
