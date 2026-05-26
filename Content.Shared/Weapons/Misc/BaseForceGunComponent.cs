using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Misc;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class BaseForceGunComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("lineColor"), AutoNetworkedField]
    public Color LineColor = Color.Orange;

    /// <summary>
    /// The entity the tethered target has a joint to.
    /// </summary>
    [DataField("tetherEntity"), AutoNetworkedField]
    public NetEntity? TetherEntity { get; set; }

    /// <summary>
    /// The entity the tethered target has a joint to.
    /// </summary>
    [DataField("tetherMirrorEntity"), AutoNetworkedField]
    public NetEntity? TetherMirrorEntity { get; set; }

    /// <summary>
    /// The entity currently tethered.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("tethered"), AutoNetworkedField]
    public NetEntity? Tethered { get; set; }

    /// <summary>
    /// Can the tethergun unanchor entities.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("canUnanchor"), AutoNetworkedField]
    public bool CanUnanchor = false;

    [ViewVariables(VVAccess.ReadWrite), DataField("canTetherAlive"), AutoNetworkedField]
    public bool CanTetherAlive = false;

    /// <summary>
    /// Max force between the tether entity and the tethered target.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("maxForce"), AutoNetworkedField]
    public float MaxForce = 200f;

    /// <summary>
    /// Frequency of the spring which pulls the object towards the tether entity.
    /// Higher number means faster pull limited my max force.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("frequency"), AutoNetworkedField]
    public float Frequency = 10f;

    /// <summary>
    /// Damping ratio of the spring which pulls the object towards the tether entity.
    /// 1 means critical damping, so perfect pull with no overshoot.
    /// >1 means overdamping. It will move slower.
    /// <1 means underdamping. It will overshoot and bounce back and forth
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("dampingRatio"), AutoNetworkedField]
    public float DampingRatio = 2f;

    /// <summary>
    /// Maximum amount of mass a tethered entity can have.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("massLimit"), AutoNetworkedField]
    public float MassLimit = 100f;

    /// <summary>
    /// Max Distance away from player the object can be before the beam breaks
    /// Objects also can't be picked up from this far away
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("maxBeamLength"), AutoNetworkedField]
    public float MaxBeamLength = 20f;

    /// <summary>
    /// Max Distance away from player the object can be moved to
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("maxDistance"), AutoNetworkedField]
    public float MaxDistance = 10f;

    /// <summary>
    /// Max Distance away from player the object can be moved to
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("reverseForce"), AutoNetworkedField]
    public bool ReverseForce = true;

    [ViewVariables(VVAccess.ReadWrite), DataField("sound"), AutoNetworkedField]
    public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/Weapons/weoweo.ogg")
    {
        Params = AudioParams.Default.WithLoop(true).WithVolume(-8f),
    };

    public EntityUid? Stream;
}
