using Content.Shared.DeviceLinking;
using Robust.Shared.Prototypes;

namespace Content.Server.Disposal.Tube;

[RegisterComponent, Access(typeof(DisposalSignalerSystem))]

public sealed partial class DisposalSignalerComponent : Component
{
    [DataField]
    public ProtoId<SourcePortPrototype> Port = "ItemDetected";
}
