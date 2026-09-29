namespace Content.Client.VendingMachines;

public enum VendingMachineVisualState : byte
{
    Normal,
    Off,
    Broken,
    Eject,
    Deny
}

public enum VendingMachineVisualLayers : byte
{
    /// <summary>
    /// Off / Broken. The other layers will overlay this if the machine is on.
    /// </summary>
    Base,

    /// <summary>
    /// Normal / Deny / Eject
    /// </summary>
    BaseUnshaded,

    /// <summary>
    /// Screens that are persistent (where the machine is not off or broken)
    /// </summary>
    Screen
}
