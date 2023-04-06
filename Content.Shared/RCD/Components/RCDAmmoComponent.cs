namespace Content.Shared.RCD.Components;

[RegisterComponent]
public sealed class RCDAmmoComponent : Component
{
    /// <summary>
    /// How many charges are contained in this ammo cartridge.
    /// Can be partially transferred into an RCD, until it is empty then it gets deleted.
    /// </summary>
    [DataField("charges"), ViewVariables(VVAccess.ReadWrite)]
    public int Charges = 5;
}

// TODO: state??? check if it desyncs
