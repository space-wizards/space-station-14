namespace Content.Client.AlertLevel;

[RegisterComponent]
public sealed class AlertLevelDisplayComponent : Component
{
    public readonly Dictionary<string, string> AlertVisuals = new();
}
