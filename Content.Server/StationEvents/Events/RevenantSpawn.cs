namespace Content.Server.StationEvents.Events;

public sealed class RevenantSpawn : StationEventSystem
{
    public override string Prototype => "RevenantSpawn";
    private static readonly string RevenantPrototype = "MobRevenant";

    public override void Started()
    {
        base.Started();

        if (TryFindRandomTile(out _, out _, out _, out var coords))
        {
            Sawmill.Info($"Spawning revenant at {coords}");
            EntityManager.SpawnEntity(RevenantPrototype, coords);
        }
    }
}
