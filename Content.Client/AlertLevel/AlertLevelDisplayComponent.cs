namespace Content.Client.AlertLevel;

[RegisterComponent]
public sealed class AlertLevelDisplayComponent : Component
{
    [DataField("alertVisuals")]
    public readonly Dictionary<string, string> AlertVisuals = new();
}
