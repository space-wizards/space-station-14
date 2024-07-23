﻿using Content.Server.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Components;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Nutrition.Components;

/// <summary>
/// This is used for a machine that extracts hunger from entities and creates meat. Yum!
/// </summary>
[RegisterComponent, Access(typeof(FatExtractorSystem)), AutoGenerateComponentPause]
public sealed partial class FatExtractorComponent : Component
{
    /// <summary>
    /// Whether or not the extractor is currently extracting fat from someone
    /// </summary>
    [DataField("processing")]
    public bool Processing = true;

    /// <summary>
    /// How much nutrition is extracted per second.
    /// </summary>
    [DataField("nutritionPerSecond"), ViewVariables(VVAccess.ReadWrite)]
    public int NutritionPerSecond = 10;

    /// <summary>
    /// An accumulator which tracks extracted nutrition to determine
    /// when to spawn a meat.
    /// </summary>
    [DataField("nutrientAccumulator"), ViewVariables(VVAccess.ReadWrite)]
    public int NutrientAccumulator;

    /// <summary>
    /// How high <see cref="NutrientAccumulator"/> has to be to spawn meat
    /// </summary>
    [DataField("nutrientPerMeat"), ViewVariables(VVAccess.ReadWrite)]
    public int NutrientPerMeat = 30;

    /// <summary>
    /// Meat spawned by the extractor.
    /// </summary>
    [DataField("meatPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>)), ViewVariables(VVAccess.ReadWrite)]
    public string MeatPrototype = "FoodMeat";

    /// <summary>
    /// When the next update will occur
    /// </summary>
    [DataField("nextUpdate", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    [AutoPausedField]
    public TimeSpan NextUpdate;

    /// <summary>
    /// How long each update takes
    /// </summary>
    [DataField("updateTime"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan UpdateTime = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The sound played when extracting
    /// </summary>
    [DataField("processSound")]
    public SoundSpecifier? ProcessSound;

    public EntityUid? Stream;

    /// <summary>
    /// A minium hunger threshold for extracting nutrition.
    /// Ignored when emagged.
    /// </summary>
    [DataField("minHungerThreshold")]
    public HungerThreshold MinHungerThreshold = HungerThreshold.Okay;
}
