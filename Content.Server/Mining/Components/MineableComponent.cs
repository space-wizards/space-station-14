using System.Threading;
using Content.Shared.Storage;

namespace Content.Server.Mining.Components;

[RegisterComponent]
[Friend(typeof(MineableSystem))]
public sealed class MineableComponent : Component
{
    [DataField("ores")] public List<EntitySpawnEntry> Ores = new();
    public float BaseMineTime = 1.0f;
}
