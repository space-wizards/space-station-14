// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.DeadSpace.Necromorphs.NecroWall.Components;

[RegisterComponent]
public sealed partial class NecroKudzuComponent : Component
{
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan NextTick = TimeSpan.FromSeconds(0);

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public float Duration = 60f;

    [DataField("proto", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    [ViewVariables(VVAccess.ReadOnly)]
    public string NecroWallId = "NecroWall";
}
