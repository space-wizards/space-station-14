// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.DeadSpace.Demons.Shadowling;

[RegisterComponent, NetworkedComponent]
public sealed partial class ShadowlingBlackMedComponent : Component
{
    [DataField] public EntProtoId ActionBlackMed = "ActionShadowlingBlackMed";
    [DataField] public EntityUid? ActionBlackMedEntity;
    [DataField] public int RequiredSlaves = 18;
    [DataField] public int MinRequiredSlaves = 12;
    [DataField] public int MaxRequiredSlaves = 18;
    [DataField] public float Duration = 2f;
}

public sealed partial class ShadowlingBlackMedEvent : EntityTargetActionEvent { }

[Serializable, NetSerializable]
public sealed partial class ShadowlingBlackMedDoAfterEvent : SimpleDoAfterEvent { }