// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeadSpace.Clothing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MultiClothingComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Force;

    [DataField, AutoNetworkedField]
    public Dictionary<string, EntProtoId> Equipment = new();

    [DataField, AutoNetworkedField]
    public Dictionary<string, EntityUid> SpawnedItems = new();

    [DataField, AutoNetworkedField]
    public Dictionary<string, EntityUid> ForcedOffItems = new();
}
