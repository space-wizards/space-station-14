using Content.Shared.Chemistry.Reaction.Events;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chemistry.Reaction.Prototypes;

public abstract class BaseReactionPrototype : IComparable<ReactionPrototype>
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public string Name { get; private set; } = string.Empty;

    [DataField(required: true)]
    public float Rate = 1;

    /// <summary>
    /// Reactants that must be present for the reaction to take place
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> Reactants = new();

    /// <summary>
    /// Any Catalysts that must be present for the reaction to take place
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<ReagentPrototype>, FixedPoint2>? Catalysts = null;

    /// <summary>
    /// If true, this reaction will only consume only integer multiples of the reactant amounts. If there are not
    /// enough reactants, the reaction does not occur. Useful for spawn-entity reactions (e.g. creating cheese).
    /// </summary>
    [DataField]
    public bool Quantized = false;

    /// <summary>
    /// Determines the order in which reactions occur. This should used to ensure that (in general) descriptive /
    /// pop-up generating and explosive reactions occur before things like foam/area effects.
    /// </summary>
    [DataField]
    public int Priority = 0;

    /// <summary>
    /// Audio to play when this absorption occurs. If audio is enabled
    /// </summary>
    [DataField("sound")]
    public SoundSpecifier? Sound = null;


    /// <summary>
    /// How dangerous are the effects of absorbing these reagents?
    /// If this is null, do not log anything
    /// </summary>
    [DataField( serverOnly: true)]
    public LogImpact? Impact = null;

    /// <summary>
    /// Minimum temperature the reaction will take place at
    /// </summary>
    [DataField]
    public float MinTemp = 0;

    /// <summary>
    /// Maximum temperature the reaction will take place at
    /// </summary>
    [DataField]
    public float MaxTemp = float.PositiveInfinity;

    /// <summary>
    /// Should absorbing these reagents transfer their heat to the absorbing entity.
    /// In most cases this should be false because entities that care about heat tend to handle a solution's heat themselves.
    /// If you enable this in that case, it will result in heat being transferred twice. Once by the system the entity's heat
    /// and solutions, and again when the solution is absorbed.
    /// </summary>
    [DataField]
    public bool TransferHeat = false;

    /// <summary>
    /// What conditions are required to allow this reaction
    /// </summary>
    [DataField]
    public List<ChemicalCondition>? Conditions = null;

    /// <summary>
    /// What effects does absorbing this reagent have
    /// </summary>
    [DataField]
    public List<ChemicalEffect>? Effects = null;

    /// <summary>
    /// Effects to be triggered when the reagents are absorbed
    /// </summary>
    [DataField]
    public List<ReagentEffect>? ReagentEffects = null;

    /// <summary>
    ///     Comparison for creating a sorted set of reactions. Determines the order in which reactions occur.
    /// </summary>
    public int CompareTo(ReactionPrototype? other)
    {
        if (other == null)
            return -1;

        if (Priority != other.Priority)
            return other.Priority - Priority;

        return string.Compare(ID, other.ID, StringComparison.Ordinal);
    }
}

public interface IReactionData : IComparable<IReactionData>
{
    float Rate { get; }
    int Priority { get; }
    List<(ProtoId<ReagentPrototype>, FixedPoint2)> Reactants { get; }
    List<(ProtoId<ReagentPrototype>, FixedPoint2)>? Catalysts { get; }
    bool Quantized { get; }
    float MinTemp { get; }
    float MaxTemp{ get; }
    bool TransferHeat{ get; }
    string ProtoId { get; }
    LogImpact? Impact { get; }
    List<ChemicalCondition>? Conditions { get; }
    List<ChemicalEffect>? Effects { get; }
    List<ReagentEffect>? ReagentEffects { get; }

    SoundSpecifier? Sound { get; }
}
