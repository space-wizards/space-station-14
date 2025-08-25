using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Body.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(StomachSystem), typeof(FoodSystem))]
public sealed partial class StomachComponent : Component
{
    /// <summary>
    /// The next time that the stomach will try to digest its contents.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextUpdate;

    /// <summary>
    /// The interval at which this stomach digests its contents.
    /// </summary>
    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Multiplier applied to <see cref="DigestionDelay"/> for adjusting based
    /// on metabolic rate multiplier.
    /// </summary>
    [DataField]
    public float DigestionDelayMultiplier = 1f;

    /// <summary>
    /// The solution inside of this stomach this transfers reagents to the body.
    /// </summary>
    [ViewVariables]
    public Entity<SolutionComponent>? Solution;

    /// <summary>
    /// What solution should this stomach push reagents into, on the body?
    /// </summary>
    [DataField]
    public string BodySolutionName = "chemicals";

    /// <summary>
    /// Time between reagents being ingested and them being
    /// transferred to <see cref="BloodstreamComponent"/>
    /// </summary>
    [DataField]
    public TimeSpan DigestionDelay = TimeSpan.FromSeconds(20);

    /// <summary>
    /// Adjusted digestion delay based off of the multiplier value.
    /// </summary>
    [ViewVariables]
    public TimeSpan AdjustedDigestionDelay => DigestionDelay * DigestionDelayMultiplier;

    /// <summary>
    /// A whitelist for what special-digestible-required foods this stomach is capable of eating.
    /// </summary>
    [DataField]
    public EntityWhitelist? SpecialDigestible = null;

    /// <summary>
    /// Controls whitelist behavior. If true, this stomach can digest
    /// <i>only</i> food that passes the whitelist. If false, it can digest
    /// normal food <i>and</i> any food that passes the whitelist.
    /// </summary>
    [DataField]
    public bool IsSpecialDigestibleExclusive = true;

    /// <summary>
    /// Used to track when each portion of a reagent should be digested.
    /// </summary>
    /// <remarks>
    /// This has custom pause/offset handling/cheating. See also
    /// <see href="https://github.com/space-wizards/RobustToolbox/issues/3768"/>.
    /// </remarks>
    [DataField]
    public Dictionary<ReagentId, List<ReagentDelta>> ReagentDeltas = new();
}

/// <summary>
/// Convenience struct for some amount of a reagent and the time that it should
/// be digested at.
/// </summary>
public record struct ReagentDelta(ReagentQuantity Reagent, TimeSpan DigestionTime);

