// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.DeadSpace.Divader;

[RegisterComponent]
public sealed partial class DivaderComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("mobDivaderRH", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string RHMobSpawnId = "MobDivaderRH";

    [ViewVariables(VVAccess.ReadWrite), DataField("mobDivaderH", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string HMobSpawnId = "MobDivaderH";

    [ViewVariables(VVAccess.ReadWrite), DataField("mobDivaderLH", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string LHMobSpawnId = "MobDivaderLH";
}
