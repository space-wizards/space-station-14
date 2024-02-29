using Robust.Shared.Prototypes;
using Content.Server.AlertLevel;

namespace Content.Shared.AlertLevelOnPress;

public abstract partial class SharedAlertLevelOnPressSystem : EntitySystem
{
}

[RegisterComponent]
[Access(typeof(SharedAlertLevelOnPressSystem))]
public sealed partial class AlertLevelOnPressComponent : Component
{
    [DataField(required: true)]
    public ProtoId<AlertLevelPrototype> AlertLevelOnActivate = string.Empty;
}
