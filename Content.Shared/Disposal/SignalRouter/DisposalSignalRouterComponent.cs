using Content.Shared.DeviceLinking;
using Content.Shared.Disposal.SignalRouter;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Disposal.Tube;

/// <summary>
/// Disposal tubes with this component can switched to route entities in
/// different directions by receiving signals from other devices.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(DisposalSignalRouterSystem))]
public sealed partial class DisposalSignalRouterComponent : Component
{
    /// <summary>
    /// Whether to route items to the side or not.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Routing;

    /// <summary>
    /// Port that sets the router to send items to the side.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> OnPort = "On";

    /// <summary>
    /// Port that sets the router to send items ahead.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> OffPort = "Off";

    /// <summary>
    /// Port that toggles the router between sending items to the side and ahead.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> TogglePort = "Toggle";
}
