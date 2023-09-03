using Robust.Shared.Prototypes;

namespace Content.Shared.Procedural;

[Prototype("salvageDifficulty")]
public sealed class SalvageDifficultyPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = string.Empty;

    /// <summary>
    /// Color to be used in UI.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("color")]
    public Color Color = Color.White;

    /// <summary>
    /// How much loot this difficulty is allowed to spawn.
    /// </summary>
    [DataField("lootBudget", required : true)]
    public float LootBudget;

    /// <summary>
    /// How many mobs this difficulty is allowed to spawn.
    /// </summary>
    [DataField("mobBudget", required : true)]
    public float MobBudget;

    /// <summary>
    /// Budget allowed for mission modifiers like no light, etc.
    /// </summary>
    [DataField("modifierBudget")]
    public float ModifierBudget;

    [DataField("recommendedPlayers", required: true)]
    public int RecommendedPlayers;
}
