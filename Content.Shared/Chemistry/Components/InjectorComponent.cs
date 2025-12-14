using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Components;

/// <summary>
/// Implements draw/inject behavior for droppers and syringes.
/// </summary>
/// <remarks>
/// Can optionally support both
/// injection and drawing or just injection. Can inject/draw reagents from solution
/// containers, and can directly inject into a mobs bloodstream.
/// </remarks>
/// <seealso cref="InjectorSystem"/>
/// <seealso cref="InjectorToggleMode"/>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class InjectorComponent : Component
{
    /// <summary>
    /// The solution to draw into or inject from.
    /// </summary>
    [DataField]
    public string SolutionName = "injector";

    /// <summary>
    /// A cached reference to the solution.
    /// </summary>
    [ViewVariables]
    public Entity<SolutionComponent>? Solution = null;

    /// <summary>
    /// The transfer amounts for the set-transfer verb.
    /// </summary>
    [DataField]
    public List<FixedPoint2> TransferAmounts = new() { 1, 5, 10, 15 };

    /// <summary>
    /// Amount to inject or draw on each usage.
    /// </summary>
    /// <remarks>
    /// If its set null, this injector is marked to inject its entire contents upon usage.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public FixedPoint2? CurrentTransferAmount = FixedPoint2.New(5);

    /// <summary>
    /// Injection delay (seconds) when the target is a mob.
    /// </summary>
    /// <remarks>
    /// The base delay has a minimum of 1 second, but this will still be modified if the target is incapacitated or
    /// in combat mode.
    /// </remarks>
    [DataField]
    public TimeSpan InjectTime = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The delay to draw reagents using the hypospray.
    /// If set, <see cref="RefillableSolutionComponent"/> RefillTime should probably have the same value.
    /// </summary>
    [DataField]
    public TimeSpan DrawTime = TimeSpan.Zero;

    /// <summary>
    /// Each additional 1u after first 5u increases the delay by X seconds.
    /// </summary>
    [DataField]
    public TimeSpan DelayPerVolume = TimeSpan.FromSeconds(0.1);

    /// <summary>
    /// Each additional 1u after first 5u increases the delay by X seconds.
    /// </summary>
    [DataField]
    public FixedPoint2 IgnoreDelayForVolume = FixedPoint2.New(5);

    /// <summary>
    /// Whether the injector is able to draw from or inject from mobs.
    /// </summary>
    /// <example>
    /// Droppers ignore mobs.
    /// </example>
    [DataField]
    public bool IgnoreMobs;

    /// <summary>
    /// Whether the injector is able to draw from or inject into containers that are closed/sealed.
    /// </summary>
    /// <example>
    /// Droppers can't inject into closed cans.
    /// </example>
    [DataField]
    public bool IgnoreClosed = true;

    /// <summary>
    /// What message will be displayed to the user when attempting to inject someone.
    /// </summary>
    /// <remarks>
    /// This is used for when you aren't injecting with a needle or an instant hypospray.
    /// It would be weird if someone injects with a spray, but the popup says "needle".
    /// </remarks>
    [DataField]
    public string PreparingInjectorUser = "injector-component-injecting-user";

    /// <summary>
    /// What message will be displayed to the target when someone attempts to inject into them.
    /// </summary>
    [DataField]
    public string PreparingInjectorTarget = "injector-component-injecting-target";

    /// <summary>
    /// The state of the injector. Determines it's attack behavior. Containers must have the
    /// right SolutionCaps to support injection/drawing. For InjectOnly injectors this should
    /// only ever be set to Inject
    /// </summary>
    [DataField, AutoNetworkedField]
    public InjectorToggleMode ToggleState = InjectorToggleMode.Draw;

    /// <summary>
    /// The state of the injector. Injection/Drawing behavior. Containers must have the
    /// right SolutionCaps to support injection/drawing. For InjectOnly injectors this should
    /// only ever be set to Inject
    /// </summary>
    [DataField]
    public InjectorToggleMode AllowedModes = InjectorToggleMode.Draw | InjectorToggleMode.Inject;

    /// <summary>
    /// Reagents that are allowed to be within this injector.
    /// If a solution has both allowed and non-allowed reagents, only allowed reagents will be drawn into this injector.
    /// A null ReagentWhitelist indicates all reagents are allowed.
    /// </summary>
    [DataField]
    public List<ProtoId<ReagentPrototype>>? ReagentWhitelist = null;

    /// <summary>
    ///     Sound that will be played when injecting.
    /// </summary>
    [DataField]
    public SoundSpecifier? InjectSound;

    /// <summary>
    /// A popup for the target upon a successful injection.
    /// It's imperative that this is not null when <see cref="InjectTime"/> is instant.
    /// </summary>
    [DataField]
    public string? InjectPopupTarget;

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
[Serializable, NetSerializable, Flags]
public enum InjectorToggleMode
{
    /// <summary>
    /// The injector will try to inject reagent into things.
    /// </summary>
    Inject = 1 << 0,

    /// <summary>
    /// The injector will try to draw reagent from things.
    /// </summary>
    Draw = 1 << 1,

    /// <summary>
    /// The injector will draw from containers and inject into mobs.
    /// </summary>
    Dynamic = 1 << 2,
}

internal static class InjectorToggleModeExtensions
{
    public static bool HasAnyFlag(this InjectorToggleMode s1, InjectorToggleMode s2)
    {
        return (s1 & s2) != 0;
    }
}
