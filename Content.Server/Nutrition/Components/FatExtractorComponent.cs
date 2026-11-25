using Content.Server.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Nutrition.Components;

/// <summary>
/// This is used for a machine that extracts hunger from entities and creates meat. Yum!
/// </summary>
[RegisterComponent, Access(typeof(FatExtractorSystem)), AutoGenerateComponentPause, AutoGenerateEntityRelations]
public sealed partial class FatExtractorComponent : Component
{
    /// <summary>
    /// Whether or not the extractor is currently extracting fat from someone
    /// </summary>
    [DataField]
    public bool Processing = true;

    /// <summary>
    /// How much nutrition is extracted per second.
    /// </summary>
    [DataField]
    public int NutritionPerSecond = 10;

    /// <summary>
    /// An accumulator which tracks extracted nutrition to determine
    /// when to spawn a meat.
    /// </summary>
    [DataField]
    public int NutrientAccumulator;

    /// <summary>
    /// How high <see cref="NutrientAccumulator"/> has to be to spawn meat
    /// </summary>
    [DataField]
    public int NutrientPerMeat = 30;

    /// <summary>
    /// Meat spawned by the extractor.
    /// </summary>
    [DataField]
    public EntProtoId MeatPrototype = "FoodMeat";

    /// <summary>
    /// When the next update will occur
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextUpdate;

    /// <summary>
    /// How long each update takes
    /// </summary>
    [DataField]
    public TimeSpan UpdateTime = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The sound played when extracting
    /// </summary>
    [DataField]
    public SoundSpecifier? ProcessSound;

    [DataField, AutoRelationField]
    public EntityRelation Stream;

    /// <summary>
    /// A minium hunger threshold for extracting nutrition, as specified by a <see cref="SatiationPrototype.Thresholds"/>.
    /// Ignored when emagged.
    /// </summary>
    [DataField(required: true)]
    public SatiationValue MinHungerThreshold;
}
