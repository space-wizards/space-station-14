using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Audio;

namespace Content.Shared.Chemistry.Components;

/// <summary>
///     Component that allows an entity instantly transfer liquids by interacting with objects that have solutions.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class HyposprayComponent : Component
{
    /// <summary>
    ///     Solution that will be used by hypospray for injections.
    /// </summary>
    [DataField]
    public string SolutionName = "hypospray";

    /// <summary>
    ///     Amount of the units that will be transfered.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 TransferAmount = FixedPoint2.New(5);

    /// <summary>
    ///     Sound that will be played when injecting.
    /// </summary>
    [DataField]
    public SoundSpecifier InjectSound = new SoundPathSpecifier("/Audio/Items/hypospray.ogg");

    /// <summary>
    /// Decides whether you can inject everything or just mobs.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public bool OnlyAffectsMobs = false;

    /// <summary>
    /// If this can draw from containers in mob-only mode.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CanContainerDraw = true;

    /// <summary>
    /// Whether or not the hypospray is able to draw from containers or if it's a single use
    /// device that can only inject.
    /// </summary>
    [DataField]
    public bool InjectOnly = false;

    /// <summary>
    /// If this container requires a doafter to reset its injection volume.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool RequireReset = false;

    /// <summary>
    /// The amount the hypospray will reset to; set by SolutionContainerManager's hypospray solution when initialized.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2? ResetAmount = null;

    /// <summary>
    /// The reset doafter duration.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ResetTime = 3f;

    /// <summary>
    /// The sound that will play when the hypospray is reset.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? ResetSoundStart;

    /// <summary>
    /// The sound that will play when the hypospray is reset.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? ResetSoundEnd;
}
