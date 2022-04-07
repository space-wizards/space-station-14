using Content.Shared.Storage;

namespace Content.Server.Mining.Components;

[RegisterComponent]
[Friend(typeof(MineableSystem))]
public sealed class MineableComponent : Component
{
    [DataField("baseMineTime")]
    public float BaseMineTime = 1.0f;

    [DataField("oreChance")]
    public float OreChance = 1.0f;

    [DataField("oreTable")]
    public List<EntitySpawnEntry> OreTable = new List<EntitySpawnEntry>();
}
