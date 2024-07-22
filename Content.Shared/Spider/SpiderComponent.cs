using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Spider;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedSpiderSystem))]
public sealed partial class SpiderComponent : Component
{
    [DataField]
    public EntProtoId WebPrototype = "SpiderWeb";

    [DataField]
    public EntProtoId WebAction = "ActionSpiderWeb";

    [DataField] public EntityUid? Action;
}

public sealed partial class SpiderWebActionEvent : InstantActionEvent { }
