namespace Content.Shared.AlertLevel;

[RegisterComponent]
public sealed partial class AlertLevelDisplayComponent : Component
{
    [DataField("alertVisuals")]
    public  Dictionary<string, string> AlertVisuals = new();
}
