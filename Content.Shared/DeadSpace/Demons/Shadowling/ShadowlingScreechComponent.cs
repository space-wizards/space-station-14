// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeadSpace.Demons.Shadowling;

[RegisterComponent, NetworkedComponent]
public sealed partial class ShadowlingScreechComponent : Component
{
    [DataField] public EntProtoId ActionScreech = "ActionShadowlingScreech";
    [DataField] public EntityUid? ActionScreechEntity;

    [DataField] public float Range = 5f;
    [DataField] public float StunDuration = 4f;
    [DataField] public int RequiredSlaves = 10;
    [DataField] public int MinRequiredSlaves = 7;
    [DataField] public int MaxRequiredSlaves = 10;
}

public sealed partial class ShadowlingScreechEvent : InstantActionEvent { }