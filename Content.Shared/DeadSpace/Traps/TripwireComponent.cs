// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DoAfter;
using Content.Shared.DeviceLinking;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.DeadSpace.Traps;

[RegisterComponent]
public sealed partial class TripwireComponent : Component
{
    [DataField]
    public ProtoId<SourcePortPrototype> Port = "Pressed";

    [DataField]
    public string ImmediateTriggerKey = "timer";

    [DataField]
    public float DisarmTime = 10f;

    [DataField]
    public bool Triggered;

    [ViewVariables]
    public HashSet<EntityUid> LinkedTargets = new();
}

[Serializable, NetSerializable]
public sealed partial class TripwireDisarmDoAfterEvent : SimpleDoAfterEvent;
