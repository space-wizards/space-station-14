using Content.Shared.DeviceLinking;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Disposal.Tube;

/// <summary>
/// Disposal pipes with this component can be linked with devices to send a signal every time an item goes through the pipe
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(DisposalSignallerSystem))]
public sealed partial class DisposalSignallerComponent : Component
{
    [DataField]
    public ProtoId<SourcePortPrototype> Port = "ItemDetected";
}
