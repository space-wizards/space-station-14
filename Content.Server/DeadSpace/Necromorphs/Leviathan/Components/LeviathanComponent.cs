// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.DeadSpace.Necromorphs.Leviathan.Components;

[RegisterComponent]
public sealed partial class LeviathanComponent : Component
{
    [DataField("proto", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    [ViewVariables(VVAccess.ReadOnly)]
    public string GhostLeviathanId = "MobGhostLeviathanNecro";

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? GhostLeviathanEntity = null;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool MindFlag = false;
}
