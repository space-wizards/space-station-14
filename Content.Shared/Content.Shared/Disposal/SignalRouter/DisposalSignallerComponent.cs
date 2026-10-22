using Content.Shared.DeviceLinking;
using Content.Shared.Disposal.SignalRouter;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Disposal.Tube;

/// <summary>
/// Disposal pipes with this component can be linked with devices to
/// send a signal every time a disposal holder goes through it.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(DisposalSignallerSystem))]
public sealed partial class DisposalSignallerComponent : Component
{
    /// <summary>
    /// The port that a signal will be emitted each time a
    /// disposal holder passes through the pipe.
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> Port = "ItemDetected";
}
