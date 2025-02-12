namespace Content.Server.Pointing.Components;

/// <summary>
/// Causes pointing arrows to go mode and murder this entity.
/// </summary>
[RegisterComponent]
public sealed partial class PointingArrowAngeringComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public int RemainingAnger = 5;
}
