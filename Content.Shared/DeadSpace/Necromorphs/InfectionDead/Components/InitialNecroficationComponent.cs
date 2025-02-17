// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Shared.DeadSpace.Necromorphs.InfectionDead.Prototypes;

namespace Content.Shared.DeadSpace.Necromorphs.InfectionDead.Components;

[RegisterComponent]
public sealed partial class InitialNecroficationComponent : Component
{

    [DataField("necroPrototype", required: false, customTypeSerializer: typeof(PrototypeIdSerializer<NecromorfPrototype>))]
    public string? NecroPrototype { get; set; } = null;

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan StartTick;
}

[ByRefEvent]
public readonly record struct StartNecroficationEvent();
