// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Shared.DeadSpace.Necromorphs.InfectionDead.Prototypes;

namespace Content.Server.DeadSpace.Necromorphs.Unitology.Abilities;

[RegisterComponent]
public sealed partial class UnitologyObeliskSpawnAbilityComponent : Component
{
    [DataField("obeliskPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ObeliskPrototype = "StructureObelisk";

    [DataField("ObeliskAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? ObeliskAction = "ActionUnitologObeliskSpawn";

    [DataField("ObeliskActionEntity")]
    public EntityUid? ObeliskActionEntity;

    public TimeSpan ObeliskSpawnDuration = TimeSpan.FromSeconds(60);

    [DataField]
    public int CountVictims = 1;

    [DataField]
    public int CountEnslaves = 2;

    public List<EntityUid> VictimsUidList = new();

    [DataField("afterGibNecroPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<NecromorfPrototype>))]
    public string AfterGibNecroPrototype = "NecroCorpseCollector";

    [DataField("afterGibEnslavedNecroPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<NecromorfPrototype>))]
    public string AfterGibEnslavedNecroPrototype = "NecroTwitcherLvl2";
}
