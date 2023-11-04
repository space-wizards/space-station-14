namespace Content.Server.Ame.Components;

/// <summary>
/// An antimatter containment cell used to handle the fuel for the AME.
/// TODO: network and put in shared
/// </summary>
[RegisterComponent]
public sealed partial class AmeFuelContainerComponent : Component
{
    /// <summary>
    /// The amount of fuel in the jar.
    /// </summary>
    [DataField("fuelAmount")]
    [ViewVariables(VVAccess.ReadWrite)]
    public int FuelAmount = 1000;

    /// <summary>
    /// The maximum fuel capacity of the jar.
    /// </summary>
    [DataField("fuelCapacity")]
    [ViewVariables(VVAccess.ReadWrite)]
    public int FuelCapacity = 1000;
}
