namespace Content.Shared.SubFloor;

// Don't need to network
/// <summary>
/// Added to anyone using <see cref="TRayScannerComponent"/> to handle the vismask changes.
/// </summary>
[RegisterComponent]
public sealed partial class TRayScannerUserComponent : Component
{
    /// <summary>
    /// How many t-rays the user is currently using.
    /// </summary>
    [DataField]
    public int Count;
}
