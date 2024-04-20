using Content.Shared.Chemistry.Reaction.Events;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Shared.Chemistry.Reaction.Prototypes;

[Prototype]
public sealed partial class AbsorptionPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Minimum amount of reagents required in the solution for the absorption to occur
    /// The absorption counts as a "combo" absorption if there is more than 1 reagent and it will receive a priority bump.
    /// You must define at least 1 reagent in this list or this prototype is invalid.
    /// </summary>
    [DataField(required:true ,customTypeSerializer:typeof(PrototypeIdDictionarySerializer<FixedPoint2,ReagentPrototype>))]
    public Dictionary<string, FixedPoint2> Reagents = new();

    /// <summary>
    /// Optional: define reagents that are required for this absorption to occur, these reagents will not be absorbed.
    /// This allows for absorption to be limited by the amount of catalyst present in the solution.
    /// If a catalyst is present, this absorption is always treated as a "combo" absorption and receives a priority bump.
    /// </summary>
    [DataField(customTypeSerializer:typeof(PrototypeIdDictionarySerializer<FixedPoint2,ReagentPrototype>))]
    public Dictionary<string, FixedPoint2>? Catalysts = null;

    /// <summary>
    /// If true, this absorption will only consume only integer multiples of the reactant amounts. This will also convert
    /// catalyst amounts into integers. If there are not enough reactants, the absorption does not occur.
    /// Useful for spawn-entity reactions (e.g. creating cheese).
    /// </summary>
    [DataField] public bool Quantized = false;

    /// <summary>
    /// Will this reagent be transferred to a target solution if possible, or will it always be deleted on absorption?
    /// </summary>
    [DataField] public bool CanTransfer = true;

    /// <summary>
    /// How fast are the reagents absorbed per second. This is used in combination with requiredReagents to calculate how much
    /// reagent is required/absorbed per update. This also is combined with the catalyst values when checking to limit reaction
    /// rate by catalysts.
    /// </summary>
    [DataField]
    public float Rate = 1.0f;


    /// <summary>
    /// What effects does absorbing this reagent have
    /// </summary>
    [DataField]
    public List<BaseSolutionEffect> Effects = new();

    /// <summary>
    /// Effects to be triggered when the reagents are absorbed
    /// </summary>
    [DataField]
    public List<ReagentEffect> ReagentEffects = new();

    /// <summary>
    /// The Minimum temperature required to absorb the reagent(s)
    /// </summary>
    [DataField]
    public float MinTemp = 0.0f;

    /// <summary>
    /// The Maximum temperature required to absorb the reagent(s)
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
    public bool TransferHeat;


    /// <summary>
    /// Determines the order in which reactions occur. This should used to ensure that (in general) descriptive /
    /// pop-up generating and explosive reactions occur before things like foam/area effects.
    /// </summary>
    [DataField("priority")]
    public int BasePriority = 0;


    /// <summary>
    /// Determines the order in which reactions occur. This should used to ensure that (in general) descriptive /
    /// pop-up generating and explosive reactions occur before things like foam/area effects.
    /// </summary>
    [DataField]
    public int ComboPriorityBump = 10;

    /// <summary>
    /// Audio to play when this absorption occurs. If audio is enabled
    /// </summary>
    [DataField("sound")]
    public SoundSpecifier? Sound = null;


    //Apply priority bumping if this is a combo absorption
    public int Priority => BasePriority + (IsComboAbsorption? 0 : 1) * ComboPriorityBump;

    /// <summary>
    /// How dangerous are the effects of absorbing these reagents?
    /// If this is null, do not log anything
    /// </summary>
    [DataField( serverOnly: true)]
    public LogImpact? Impact = null;

    public bool IsComboAbsorption => Catalysts is {Count: > 0} || Reagents.Count > 1;
}
