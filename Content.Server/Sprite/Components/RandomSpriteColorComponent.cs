namespace Content.Server.Sprite.Components
{
    [RegisterComponent]
    public sealed class RandomSpriteColorComponent : Component
    {
        // This should handle random states + colors for layers.
        // Saame with RandomSpriteState
        [DataField("selected")] public string? SelectedColor;
        [DataField("state")] public string BaseState = "error";

        [DataField("colors")] public readonly Dictionary<string, Color> Colors = new();
    }
}
