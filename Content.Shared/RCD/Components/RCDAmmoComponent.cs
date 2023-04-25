using Content.Shared.RCD.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.RCD.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RCDAmmoSystem))]
public sealed partial class RCDAmmoComponent : Component
{
    /// <summary>
    /// How many charges are contained in this ammo cartridge.
    /// Can be partially transferred into an RCD, until it is empty then it gets deleted.
    /// </summary>
    [DataField("charges"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public int Charges = 5;
}

// TODO: state??? check if it desyncs
