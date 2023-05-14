using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Misc;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TetherGunComponent : Component
{
    /// <summary>
    /// The entity the tethered target has a joint to.
    /// </summary>
    [DataField("tetherEntity"), AutoNetworkedField]
    public EntityUid? TetherEntity;

    /// <summary>
    /// The entity currently tethered.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("tethered"), AutoNetworkedField]
    public EntityUid? Tethered;

    /// <summary>
    /// Can the tethergun unanchor entities.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("canUnanchor"), AutoNetworkedField]
    public bool CanUnanchor = false;

    /// <summary>
    /// Maximum amount of mass a tethered entity can have.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("massLimit"), AutoNetworkedField]
    public float MassLimit = 30f;
}
