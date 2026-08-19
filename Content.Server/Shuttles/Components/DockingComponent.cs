using Content.Shared.Shuttles.BUIStates;
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
    /// Color that gets shown on the radar screen.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public Color RadarColor = Color.DarkViolet;

    /// <summary>
    /// Color that gets shown on the radar screen in DOCK tab when the dock is highlighted
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public Color HighlightedRadarColor = Color.Magenta;

    [ViewVariables]
    public int PathfindHandle = -1;

    /// <summary>
    /// The category of the dock, used to determine what kind of dock it is for the purposes of the radar screen legend
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public LegendCategory DockLegendCategory = LegendCategory.General;
}
