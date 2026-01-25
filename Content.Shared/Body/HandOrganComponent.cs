using Content.Shared.Hands.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.Body;

[RegisterComponent, NetworkedComponent]
[Access(typeof(HandOrganSystem))]
public sealed partial class HandOrganComponent : Component
{
    [DataField(required: true)]
    public string HandID;

    [DataField(required: true)]
    public Hand Data;
}
