using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Misc;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class COTelekinesisHandComponent : Component
{
    [ViewVariables, DataField, AutoNetworkedField]
    public Color LineColor = Color.Aquamarine;

    /// <summary>
    /// Can the tethergun unanchor entities.
    /// </summary>
    [ViewVariables, DataField, AutoNetworkedField]
    public bool CanUnanchor = false;

    [ViewVariables, DataField, AutoNetworkedField]
    public bool CanTetherAlive = false;

    /// <summary>
    /// Max force between the tether entity and the tethered target.
    /// </summary>
    [ViewVariables, DataField, AutoNetworkedField]
    public float MaxForce = 200f;

    [ViewVariables, DataField, AutoNetworkedField]
    public float Frequency = 10f;

    [ViewVariables, DataField, AutoNetworkedField]
    public float DampingRatio = 2f;

    /// <summary>
    /// Maximum amount of mass a tethered entity can have.
    /// </summary>
    [ViewVariables, DataField, AutoNetworkedField]
    public float MassLimit = 100f;

    [ViewVariables, DataField, AutoNetworkedField]
    public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/Weapons/weoweo.ogg")
    {
        Params = AudioParams.Default.WithLoop(true).WithVolume(-8f),
    };

    [DataField]
    public EntityUid? Stream;

    [ViewVariables, DataField, AutoNetworkedField]
    public float MaxDistance = 10f;

    /// <summary>
    /// The entity the tethered target has a joint to.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? TetherEntity { get; set; }

    /// <summary>
    /// The entity currently tethered.
    /// </summary>
    [ViewVariables, DataField, AutoNetworkedField]
    public EntityUid? Tethered { get; set; }
}
