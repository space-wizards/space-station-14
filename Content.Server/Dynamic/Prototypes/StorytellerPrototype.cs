using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Server.Dynamic.Prototypes;

/// <summary>
///     A "dynamic preset" which is selected before round start and modifies Dynamic in some way.
/// </summary>
[Prototype("storyteller")]
public class StorytellerPrototype : IPrototype
{
    [DataField("id", required: true)]
    public string ID { get; } = default!;

    /// <summary>
    ///     How hard this storyteller is weighted against other storytellers.
    /// </summary>
    [DataField("weight")]
    public int Weight = 5;

    /// <summary>
    ///     Modifies the Lorentz curve center at roundstart.
    /// </summary>
    [DataField("roundstartCurveCenterModifier")]
    public StorytellerMinMax RoundstartCurveCenterModifier = new();

    /// <summary>
    ///     Modifies the Lorentz curve width at roundstart.
    /// </summary>
    [DataField("roundstartCurveWidthModifier")]
    public StorytellerMinMax RoundstartCurveWidthModifier = new();

    /// <summary>
    ///     Modifies the Lorentz curve center at midround.
    /// </summary>
    [DataField("midroundCurveCenterModifier")]
    public StorytellerMinMax MidroundCurveCenterModifier = new();

    /// <summary>
    ///     Modifies the Lorentz curve width at midround.
    /// </summary>
    [DataField("midroundCurveWidthModifier")]
    public StorytellerMinMax MidroundCurveWidthModifier = new();

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
public class StorytellerMinMax
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
