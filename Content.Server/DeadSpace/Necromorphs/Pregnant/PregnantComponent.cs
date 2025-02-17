// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Storage;

namespace Content.Server.DeadSpace.Necromorphs.Pregnant;

[RegisterComponent]
public sealed partial class PregnantComponent : Component
{
    [DataField("spawned", required: true)]
    public List<EntitySpawnEntry> SpawnedEntities = new();
}
