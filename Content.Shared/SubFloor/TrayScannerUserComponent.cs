namespace Content.Shared.SubFloor;

// Don't need to network
[RegisterComponent]
public sealed partial class TrayScannerUserComponent : Component
{
    [DataField]
    public int OriginalVisMask;
}
