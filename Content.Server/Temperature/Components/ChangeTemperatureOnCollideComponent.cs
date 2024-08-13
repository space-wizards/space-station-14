using Content.Server.Temperature.Systems;
using Content.Server.Temperature.Components; 
using Content.Shared.Temperature;

namespace Content.Server.Temperature.Components;


[RegisterComponent, Access(typeof(TemperatureSystem))]
public sealed partial class ChangeTemperatureOnCollideComponent : Component
{
    /// <summary>
    /// The amount it changes the target's temperature by.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Heat = 0f;

    /// <summary>
    /// If this heat change ignores heat resistance or not.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool ignoreHeatResistance = true;
}