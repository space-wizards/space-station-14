// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Shared.DeadSpace.Necromorphs.InfectionDead.Prototypes;

namespace Content.Server.DeadSpace.Necromorphs.Unitology.Abilities;

[RegisterComponent]
public sealed partial class UnitologyObeliskActivateAbilityComponent : Component
{
    [DataField("ObeliskAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? ObeliskAction = "ActionUnitologObeliskActivate";

    [DataField]
    public EntityUid? ObeliskActionEntity;

    [DataField]
    public EntityUid? Obelisk = null;

    public TimeSpan ObeliskActivateDuration = TimeSpan.FromSeconds(30);

    [DataField]
    public string IcMessage = "Мы будем едины!";

    [DataField]
    public int CountUni = 6;

    [DataField("afterGibNecroPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<NecromorfPrototype>))]
    public string AfterGibNecroPrototype = "NecroCorpseCollector";

}
