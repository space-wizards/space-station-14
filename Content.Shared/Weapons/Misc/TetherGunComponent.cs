using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Misc;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class TetherGunComponent : BaseForceGunComponent
{
    [ViewVariables(VVAccess.ReadWrite), DataField("maxDistance"), AutoNetworkedField]
    public float MaxDistance = 10f;

    /// <summary>
    /// The entity the tethered target has a joint to.
    /// </summary>
    [DataField("tetherEntity"), AutoNetworkedField]
    public override EntityUid? TetherEntity { get; set; }

    /// <summary>
    /// The entity currently tethered.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("tethered"), AutoNetworkedField]
    public override EntityUid? Tethered { get; set; }
}
