namespace Content.Server.Speech.Components;

[RegisterComponent]
public sealed partial class RussianAccentComponent : Component
{
    /// <summary>
    /// The chance (0.0 to 1.0) that articles like "the", "a", "an" will be removed from sentences.
    /// </summary>
    [DataField("articleRemovalChance")]
    public float ArticleRemovalChance = 0.8f;
}
