using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Offbrand.Skeletons;

[RegisterComponent, NetworkedComponent]
public sealed partial class DetachableOrganComponent : Component
{
    public bool Detaching = false;

    [DataField(required: true)]
    public EntProtoId DetachedBody;
}
