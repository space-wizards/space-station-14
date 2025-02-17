// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.GameStates;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.DeadSpace.Abilities.AutoInjectReagent.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class AutoInjectReagentComponent : Component
{
    [DataField("AutoInjectReagentAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? AutoInjectReagentAction = "ActionAutoInjectReagent";

    [DataField("AutoInjectReagentActionEntity")]
    public EntityUid? AutoInjectReagentActionEntity;

    [DataField("injectSound")]
    public SoundSpecifier InjectSound = new SoundPathSpecifier("/Audio/Items/hypospray.ogg");

    [DataField("reagents", required: true)]
    public Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> Reagents { get; set; } = new Dictionary<ProtoId<ReagentPrototype>, FixedPoint2>();
}
