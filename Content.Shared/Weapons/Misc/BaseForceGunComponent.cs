using Robust.Shared.Audio;

namespace Content.Shared.Weapons.Misc;

public abstract partial class BaseForceGunComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("lineColor"), AutoNetworkedField]
    public Color LineColor = Color.Orange;

    /// <summary>
    /// The entity the tethered target has a joint to.
    /// </summary>
    [DataField("tetherEntity"), AutoNetworkedField]
    public virtual EntityUid? TetherEntity { get; set; }

    /// <summary>
    /// The entity currently tethered.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("tethered"), AutoNetworkedField]
    public virtual EntityUid? Tethered { get; set; }

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

    [ViewVariables(VVAccess.ReadWrite), DataField("frequency"), AutoNetworkedField]
    public float Frequency = 10f;

    [ViewVariables(VVAccess.ReadWrite), DataField("dampingRatio"), AutoNetworkedField]
    public float DampingRatio = 2f;

    /// <summary>
    /// Maximum amount of mass a tethered entity can have.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("massLimit"), AutoNetworkedField]
    public float MassLimit = 100f;

    [ViewVariables(VVAccess.ReadWrite), DataField("sound"), AutoNetworkedField]
    public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/Weapons/weoweo.ogg")
    {
        Params = AudioParams.Default.WithLoop(true).WithVolume(-8f),
    };

    public EntityUid? Stream;
}
