using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Jaunt;

[RegisterComponent, NetworkedComponent]
public sealed partial class JauntComponent : Component
{
    [DataField]
    public EntProtoId JauntAction = "ActionPolymorphJaunt";

    public EntityUid? Action;

    // TODO: Enter & Exit Times and Whitelist when Actions are reworked and can support it
    // TODO: Cooldown pausing when Actions can support it
}
