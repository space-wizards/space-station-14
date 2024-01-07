namespace Content.Server.Cargo.Components;

[RegisterComponent]
public sealed partial class CargoTrackingComponent: Component
{
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public int? TrackedOrderId;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public string? TrackedOrderName;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public bool Waiting = false;
}
