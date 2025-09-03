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
    /// Solution that hypospray will use for injections.
    /// </summary>
    [DataField]
    public string SolutionName = "hypospray";

    /// <summary>
    /// Amount of the units that will be transferred.
    /// </summary>
    [AutoNetworkedField]
    [DataField]
    public FixedPoint2 TransferAmount = FixedPoint2.New(5);

    /// <summary>
    /// Sound that will be played when injecting.
    /// </summary>
    [DataField]
    public SoundSpecifier InjectSound = new SoundPathSpecifier("/Audio/Items/hypospray.ogg");

    /// <summary>
    /// Decides whether you can inject everything or just mobs.
    /// </summary>
    [AutoNetworkedField]
    [DataField(required: true)]
    public bool OnlyAffectsMobs = false;

    /// <summary>
    /// If this can draw from containers in mob-only mode.
    /// </summary>
    [AutoNetworkedField]
    [DataField]
    public bool CanContainerDraw = true;

    /// <summary>
    /// Whether the hypospray is able to draw from containers or if it's a single use
    /// device that can only inject.
    /// </summary>
    [DataField]
    public bool InjectOnly = false;

    #region Non-Instant Hyposprays
    /// <summary>
    /// Whether the hypospray injects it's entire capacity on use.
    /// Used by Jet Injectors.
    /// </summary>
    [DataField]
    public bool InjectMaxCapacity = false;

    /// <summary>
    /// The length of the Injection doAfter.
    /// Used by Jet Injectors.
    /// </summary>
    [DataField]
    public TimeSpan InjectTime = TimeSpan.Zero;

    /// <summary>
    /// Injection delay (seconds) when the target is a mob.
    /// </summary>
    /// <remarks>
    /// The base delay has a minimum of 1 second, but this will still be modified if the target is incapacitated or
    /// in combat mode.
    /// Used by Jet Injectors.
    /// </remarks>
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(2.5);

    /// <summary>
    /// Each additional 1u after the first 5u increases the delay by X seconds.
    /// Used by Jet Injectors.
    /// </summary>
    [DataField]
    public TimeSpan DelayPerVolume = TimeSpan.FromSeconds(0.05);

    #region Arguments for injection doafter
    /// <inheritdoc cref=DoAfterArgs.NeedHand>
    [DataField]
    public bool NeedHand = true;

    /// <inheritdoc cref=DoAfterArgs.BreakOnHandChange>
    [DataField]
    public bool BreakOnHandChange = true;

    /// <inheritdoc cref=DoAfterArgs.MovementThreshold>
    [DataField]
    public float MovementThreshold = 0.1f;

    #endregion
    #endregion
}
