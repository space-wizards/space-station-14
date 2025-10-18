using Robust.Shared.GameStates;

namespace Content.Shared.Friction;

[RegisterComponent, NetworkedComponent]
[Access(typeof(TileFrictionController)), AutoGenerateComponentState]
public sealed partial class TileFrictionModifierComponent : Component
{
    /// <summary>
    ///     Multiply the tilefriction cvar by this to get the body's actual tilefriction.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("modifier"), AutoNetworkedField]
    public float Modifier;
}
