// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.GameStates;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.DeadSpace.Abilities.HipoHand.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class HipoHandComponent : Component
{

    [DataField("injectSound")]
    public SoundSpecifier InjectSound = new SoundPathSpecifier("/Audio/Items/hypospray.ogg");

    [DataField(required: true)]
    public ProtoId<ReagentPrototype> Reagent = string.Empty;

    [DataField(required: true)]
    public FixedPoint2 Quantity;

    [DataField(required: true)]
    public FixedPoint2 MaxCountReagent;

    [DataField(required: true)]
    public FixedPoint2 CountReagent;

    [DataField]
    public float CountRegen = 5f;

    [DataField("durationRegenReagent")]
    public float DurationRegenReagent = 5f;

    [DataField("timeUntilRegenReagent", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan TimeUntilRegenReagent;
}

[ByRefEvent]
public readonly record struct RegenReagentEvent();
