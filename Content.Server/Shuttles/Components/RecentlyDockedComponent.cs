namespace Content.Server.Shuttles.Components;

/// <summary>
/// Added to <see cref="DockingComponent"/> that have recently undocked.
/// This checks for whether they've left the specified radius before allowing them to automatically dock again.
/// </summary>
[RegisterComponent]
public sealed partial class RecentlyDockedComponent : Component
{
    [DataField("lastDocked")]
    public EntityUid LastDocked;

    [ViewVariables(VVAccess.ReadWrite), DataField("radius")]
    public float Radius = 1.5f;
}
