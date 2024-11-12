using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Jaunt;

// Used to control various aspects of a Jaunt
//  Can be used in-place of giving a jaunt-action directly
[RegisterComponent, NetworkedComponent]
public sealed partial class JauntComponent : Component
{
    // Which Jaunt Action the component should grant
    [DataField]
    public EntProtoId JauntAction = "ActionPolymorphJaunt";

    // The jaunt action itself
    public EntityUid? Action;

    // TODO: Enter & Exit Times and Whitelist when Actions are reworked and can support it
    // TODO: Cooldown pausing when Actions can support it
}
