using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Prototypes;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chemistry.Components;

/// <summary>
/// Implements draw/inject behavior for droppers and syringes.
/// </summary>
/// <remarks>
/// Can optionally support both
/// injection and drawing or just injection. Can inject/draw reagents from solution
/// containers, and can directly inject into a mob's bloodstream.
/// </remarks>
/// <seealso cref="InjectorModePrototype"/>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(InjectorSystem))]
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
    /// Amount to inject or draw on each usage.
    /// </summary>
    /// <remarks>
    /// If its set null, this injector is marked to inject its entire contents upon usage.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public FixedPoint2? CurrentTransferAmount = FixedPoint2.New(5);


    /// <summary>
    /// The mode that this injector starts with on MapInit.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public ProtoId<InjectorModePrototype> ActiveModeProtoId;

    /// <summary>
    /// The possible <see cref="InjectorModePrototype"/> that it can switch between.
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<InjectorModePrototype>> AllowedModes;

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
    /// Reagents that are allowed to be within this injector.
    /// If a solution has both allowed and non-allowed reagents, only allowed reagents will be drawn into this injector.
    /// A null ReagentWhitelist indicates all reagents are allowed.
    /// </summary>
    [DataField]
    public List<ProtoId<ReagentPrototype>>? ReagentWhitelist;

    #region Arguments for injection doafter

    /// <inheritdoc cref="DoAfterArgs.NeedHand"/>
    [DataField]
    public bool NeedHand = true;

    /// <inheritdoc cref="DoAfterArgs.BreakOnHandChange"/>
    [DataField]
    public bool BreakOnHandChange = true;

    /// <inheritdoc cref="DoAfterArgs.MovementThreshold"/>
    [DataField]
    public float MovementThreshold = 0.1f;

    #endregion
}

internal static class InjectorToggleModeExtensions
{
    public static bool HasAnyFlag(this InjectorBehavior s1, InjectorBehavior s2)
    {
        return (s1 & s2) != 0;
    }
}
