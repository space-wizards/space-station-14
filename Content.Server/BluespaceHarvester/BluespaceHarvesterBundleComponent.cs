using Content.Shared.Storage;

namespace Content.Server.BluespaceHarvester;

[RegisterComponent]
public sealed partial class BluespaceHarvesterBundleComponent : Component
{
    [DataField]
    public List<EntitySpawnEntry> Contents = new();

    [DataField]
    public bool Spawned = false;
}
