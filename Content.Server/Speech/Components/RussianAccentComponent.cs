namespace Content.Server.Speech.Components;

[RegisterComponent]
public sealed partial class RussianAccentComponent : Component
{
    /// <summary>
    /// The chance (0.0 to 1.0) that articles like "the", "a", "an" will be removed from sentences, default is 80%.
    /// </summary>
    [DataField("articleRemovalChance")]
    public float ArticleRemovalChance = 0.8f;

    /// <summary>
    /// The chance (0.0 to 1.0) that "tovarisch" will be replaced with "komrade" (comrade) instead, default is 20%.
    /// </summary>
    [DataField("komradeReplacementChance")]
    public float KomradeReplacementChance = 0.2f;
}
