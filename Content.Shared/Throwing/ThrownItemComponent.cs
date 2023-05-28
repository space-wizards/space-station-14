using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Throwing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ThrownItemComponent : Component
{
    /// <summary>
    /// Was this entity lag compensated prior to being thrown, in which case remove the comp upon completion.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("lagCompensated"), AutoNetworkedField]
    public bool LagCompensated = false;

    [ViewVariables(VVAccess.ReadWrite), DataField("thrower"), AutoNetworkedField]
    public EntityUid? Thrower;
}
