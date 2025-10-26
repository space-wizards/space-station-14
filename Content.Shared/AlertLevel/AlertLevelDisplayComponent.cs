using Robust.Shared.Prototypes;

namespace Content.Shared.AlertLevel;

[RegisterComponent]
public sealed partial class AlertLevelDisplayComponent : Component
{
    [DataField]
    public Dictionary<ProtoId<AlertLevelPrototype>, string> AlertVisuals = new();
}
