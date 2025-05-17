using Robust.Shared.GameStates;

namespace Content.Shared.Puppet;

[RegisterComponent, NetworkedComponent]
public sealed partial class VentriloquistPuppetComponent : Component
{
    // Frontier edit below
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public List<LocId> RemoveHand = new ();

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public List<LocId> RemovedHand = new();

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public List<LocId> InsertHand = new ();

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public List<LocId> InsertedHand = new ();

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public List<LocId> PuppetRoleName = new ();

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public List<LocId> PuppetRoleDescription = new ();
}
