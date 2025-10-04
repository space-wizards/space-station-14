using Content.Shared.DeviceLinking;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Conduit.SignalRouter;

/// <summary>
/// Conduits with this component can be linked with devices to
/// send a signal every time an entity goes through it.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ConduitSignallerSystem))]
public sealed partial class ConduitSignallerComponent : Component
{
    /// <summary>
    /// The port that a signal will be emitted each time a
    /// entity passes through the pipe.
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> Port = "ItemDetected";
}
