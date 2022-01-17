using System.Collections.Generic;
using Content.Server.GameTicking.Commands;
using Content.Server.Storage;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;

namespace Content.Server.Mining.Components;

[RegisterComponent, ComponentProtoName("Mineable")]
[Friend(typeof(MineableSystem))]
public class MineableComponent : Component
{
    public float BaseMineTime = 1.0f;

    public float OreChance = 1.0f;

    public List<EntitySpawnEntry> OreTable = new List<EntitySpawnEntry>();
}
