// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.GameStates;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;

namespace Content.Shared.DeadSpace.Abilities.AutoInjectReagent.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class AutoInjectReagentComponent : Component
{
    [DataField]
    public EntProtoId AutoInjectReagentAction = "ActionAutoInjectReagent";

    [DataField]
    public EntityUid? AutoInjectReagentActionEntity;

    [DataField]
    public SoundSpecifier InjectSound = new SoundPathSpecifier("/Audio/Items/hypospray.ogg");

    [DataField(required: true)]
    public Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> Reagents { get; set; } = new Dictionary<ProtoId<ReagentPrototype>, FixedPoint2>();
}
