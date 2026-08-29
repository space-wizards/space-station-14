using Content.Shared.DeviceLinking;
using Robust.Shared.Prototypes;

namespace Content.Server.Shuttles.Components;

[RegisterComponent]
public sealed partial class DockingSignalControlComponent : Component
{
    /// <summary>
    /// Output port that is high while docked.
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> DockStatusSignalPort = "DockStatus";

    /// <summary>
    /// Input port that toggles the docking status
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> DockTogglePort = "DockToggle";
}
