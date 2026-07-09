using Content.Shared.Shuttles.Components;
using Robust.Shared.Physics.Dynamics.Joints;

namespace Content.Server.Shuttles.Components;

[RegisterComponent]
public sealed partial class DockingComponent : SharedDockingComponent
{
    [DataField]
    public EntityUid? DockedWith;

    [ViewVariables]
    public Joint? DockJoint;

    [DataField]
    public string? DockJointId;

    [ViewVariables]
    public override bool Docked => DockedWith != null;

    /// <summary>
    /// Color of the dock on the DOCK screen when dock is unfocused
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public Color RadarColor = Color.DarkViolet;

    /// <summary>
    /// Color that gets shown on NAV and DOCK screen when the dock is focused
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public Color HighlightedRadarColor = Color.Magenta;

    [ViewVariables]
    public int PathfindHandle = -1;
}
