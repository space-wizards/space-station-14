namespace Content.Server.AME.Components;

// TODO: network and put in shared
[RegisterComponent]
public sealed class AMEFuelContainerComponent : Component
{
    /// <summary>
    /// The amount of fuel in the jar.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("fuelAmount")]
    public int FuelAmount = 1000;

    /// <summary>
    /// The maximum fuel capacity of the jar.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("fuelCapacity")]
    public int FuelCapacity = 1000;
}
