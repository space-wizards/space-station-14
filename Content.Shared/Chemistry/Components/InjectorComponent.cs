using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Components;

[Serializable, NetSerializable]
public sealed partial class InjectorDoAfterEvent : SimpleDoAfterEvent
{
}

/// <summary>
/// Implements draw/inject behavior for droppers and syringes.
/// </summary>
/// <remarks>
/// Can optionally support both
/// injection and drawing or just injection. Can inject/draw reagents from solution
/// containers, and can directly inject into a mobs bloodstream.
/// </remarks>
/// <seealso cref="SharedInjectorSystem"/>
/// <seealso cref="InjectorToggleMode"/>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class InjectorComponent : Component
{
    [DataField]
    public string SolutionName = "injector";

    /// <summary>
    /// Whether or not the injector is able to draw from containers or if it's a single use
    /// device that can only inject.
    /// </summary>
    [DataField]
    public bool InjectOnly;

    /// <summary>
    /// Whether or not the injector is able to draw from or inject from mobs
    /// </summary>
    /// <remarks>
    ///     for example: droppers would ignore mobs
    /// </remarks>
    [DataField]
    public bool IgnoreMobs;

    /// <summary>
    ///     The minimum amount of solution that can be transferred at once from this solution.
    /// </summary>
    [DataField("minTransferAmount")]
    public FixedPoint2 MinimumTransferAmount = FixedPoint2.New(5);

    /// <summary>
    ///     The maximum amount of solution that can be transferred at once from this solution.
    /// </summary>
    [DataField("maxTransferAmount")]
    public FixedPoint2 MaximumTransferAmount = FixedPoint2.New(15);

    /// <summary>
    /// Amount to inject or draw on each usage. If the injector is inject only, it will
    /// attempt to inject it's entire contents upon use.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public FixedPoint2 TransferAmount = FixedPoint2.New(5);

    /// <summary>
    /// Injection delay (seconds) when the target is a mob.
    /// </summary>
    /// <remarks>
    /// The base delay has a minimum of 1 second, but this will still be modified if the target is incapacitated or
    /// in combat mode.
    /// </remarks>
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Each additional 1u after first 5u increases the delay by X seconds.
    /// </summary>
    [DataField]
    public TimeSpan DelayPerVolume = TimeSpan.FromSeconds(0.1);

    /// <summary>
    /// The state of the injector. Determines it's attack behavior. Containers must have the
    /// right SolutionCaps to support injection/drawing. For InjectOnly injectors this should
    /// only ever be set to Inject
    /// </summary>
    [AutoNetworkedField]
    [DataField]
    public InjectorToggleMode ToggleState = InjectorToggleMode.Draw;

    /// <summary>
    /// Reagents that are allowed to be within this injector.
    /// If a solution has both allowed and non-allowed reagents, only allowed reagents will be drawn into this injector.
    /// A null ReagentWhitelist indicates all reagents are allowed.
    /// </summary>
    [DataField]
    public List<ProtoId<ReagentPrototype>>? ReagentWhitelist = null;

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
}

/// <summary>
/// Possible modes for an <see cref="InjectorComponent"/>.
/// </summary>
public enum InjectorToggleMode : byte
{
    /// <summary>
    /// The injector will try to inject reagent into things.
    /// </summary>
    Inject,

    /// <summary>
    /// The injector will try to draw reagent from things.
    /// </summary>
    Draw
}
