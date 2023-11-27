using Content.Shared.Storage;

namespace Content.Server.BluespaceHarvester;

// TODO: Make it not tied to the harvester for mappers and loot in debris and dungeons.
[RegisterComponent]
public sealed partial class BluespaceHarvesterBundleComponent : Component
{
    [DataField]
    public List<EntitySpawnEntry> Contents = new();

    [DataField]
    public bool Spawned;
}
