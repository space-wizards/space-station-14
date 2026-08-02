// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.DeadSpace.Demons.Shadowling;

[RegisterComponent, NetworkedComponent]
public sealed partial class ShadowlingMindShieldBreakComponent : Component
{
    [DataField] public EntProtoId ActionMindShieldBreak = "ActionShadowlingMindShieldBreak";
    [DataField] public EntityUid? ActionMindShieldBreakEntity;
    [DataField] public int RequiredSlaves = 15;
    [DataField] public int MinRequiredSlaves = 10;
    [DataField] public int MaxRequiredSlaves = 15;
    [DataField] public float Duration = 4f;
    [DataField] public SoundSpecifier BreakSound = new SoundCollectionSpecifier("ShadowlingBreak");
}
public sealed partial class ShadowlingMindShieldBreakEvent : EntityTargetActionEvent { }

[Serializable, NetSerializable]
public sealed partial class ShadowlingMindShieldBreakDoAfterEvent : SimpleDoAfterEvent { }