namespace Content.Shared.SubFloor;

// Don't need to network
/// <summary>
/// Added to anyone using <see cref="TrayScannerComponent"/> to handle the vismask changes.
/// </summary>
[RegisterComponent]
public sealed partial class TrayScannerUserComponent : Component
{
    /// <summary>
    /// How many t-rays the user is currently using.
    /// </summary>
    [DataField]
    public int Count;
}
