using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Server.Dynamic.Prototypes;

/// <summary>
///     A "dynamic preset" which is selected before round start and modifies Dynamic in some way.
/// </summary>
[Prototype("storyteller")]
public sealed class StorytellerPrototype : IPrototype
{
    [DataField("id", required: true)]
    public string ID { get; } = default!;

    /// <summary>
    ///     A human readable name for this storyteller.
    ///     Shown in Storyteller votes.
    /// </summary>
    [DataField("name", required: true)]
    public string Name = default!;

    /// <summary>
    ///     How hard this storyteller is weighted against other storytellers.
    /// </summary>
    [DataField("weight")]
    public int Weight = 5;

    /// <summary>
    ///     Is this storyteller votable, or admin only?
    /// </summary>
    [DataField("votable")]
    public bool Votable = true;

    /// <summary>
    ///     Modifies the base threat pool Lorentz curve center.
    /// </summary>
    [DataField("threatCurveCenterModifier")]
    public StorytellerMinMax RoundstartCurveCenterModifier = new();

    /// <summary>
    ///     Modifies the base threat pool Lorentz curve width at roundstart.
    /// </summary>
    [DataField("threatCurveWidthModifier")]
    public StorytellerMinMax RoundstartCurveWidthModifier = new();

    /// <summary>
    ///     Modifies the lorentz curve center for divvying up between roundstart and midround budgets.
    /// </summary>
    [DataField("splitCurveCenterModifier")]
    public StorytellerMinMax SplitCurveCenterModifier = new();

    /// <summary>
    ///     Modifies the lorentz curve width for divvying up between roundstart and midround budgets.
    /// </summary>
    [DataField("splitCurveWidthModifier")]
    public StorytellerMinMax SplitCurveWidthModifier = new();

    /// <summary>
    ///     Modifies the threat cap for Dynamic.
    /// </summary>
    [DataField("threatCapModifier")]
    public StorytellerMinMax ThreatCapModifier = new();

    /// <summary>
    ///     A dictionary of <see cref="GameEventTagPrototype"/> to <see cref="StorytellerMinMax"/>
    /// </summary>
    [DataField("tagWeights", customTypeSerializer:typeof(PrototypeIdDictionarySerializer<StorytellerMinMax, GameEventTagPrototype>))]
    public Dictionary<string, StorytellerMinMax> TagWeights = new();
}

/// <summary>
///     Looks like this in YAML
///
///     roundStartCurveCenterModifier:
///         min: 0.5
///         max: 1.0
///
/// </summary>
[DataDefinition]
public sealed class StorytellerMinMax
{
    [DataField("min")]
    public float Min;

    [DataField("max")]
    public float Max;

    public float Choose(IRobustRandom random)
    {
        return (Min == 0.0f && Max == 0.0f) ? 0.0f : random.NextFloat(Min, Max);
    }
}
