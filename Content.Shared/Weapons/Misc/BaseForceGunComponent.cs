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
    public EntityUid? TetherEntity { get; set; }

    /// <summary>
    /// The mirror entity which applies an opposite force on the gun through a copy of the main joint
    /// </summary>
    [DataField("tetherMirrorEntity"), AutoNetworkedField]
    public EntityUid? TetherMirrorEntity { get; set; }

    /// <summary>
    /// The entity currently tethered.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("tethered"), AutoNetworkedField]
    public EntityUid? Tethered { get; set; }

    /// <summary>
    /// Can the tethergun unanchor entities.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("canUnanchor")]
    public bool CanUnanchor = false;

    /// <summary>
    /// Can the tethergun pick up living entities.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("canTetherAlive")]
    public bool CanTetherAlive = false;

    /// <summary>
    /// Max force between the tether entity and the tethered target.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("maxForce")]
    public float MaxForce = 200f;

    /// <summary>
    /// Frequency of the spring which pulls the object towards the tether entity.
    /// Higher number means faster pull limited my max force.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("frequency")]
    public float Frequency = 10f;

    /// <summary>
    /// Damping ratio of the spring which pulls the object towards the tether entity.
    /// 1 means critical damping, so perfect pull with no overshoot.
    /// >1 means overdamping. It will move slower.
    /// <1 means underdamping. It will overshoot and bounce back and forth
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("dampingRatio")]
    public float DampingRatio = 2f;

    /// <summary>
    /// Maximum amount of mass a tethered entity can have.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("massLimit")]
    public float MassLimit = 100f;

    /// <summary>
    /// Max Distance away from player the object can be before the beam breaks
    /// Objects also can't be picked up from this far away
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("maxBeamLength")]
    public float MaxBeamLength = 20f;

    /// <summary>
    /// Max Distance away from player the TetherEntity can be moved to
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("maxDistance")]
    public float MaxDistance = 10f;

    /// <summary>
    /// Does the tether gun apply an opposite force on itself
    /// Important to be false for admeme variants
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("reverseForce")]
    public bool ReverseForce = true;

    [ViewVariables(VVAccess.ReadWrite), DataField("sound")]
    public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/Weapons/weoweo.ogg")
    {
        Params = AudioParams.Default.WithLoop(true).WithVolume(-8f),
    };

    public EntityUid? Stream;
}
