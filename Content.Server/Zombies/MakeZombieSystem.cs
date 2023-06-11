using Content.Server.Ghost.Roles.Events;

namespace Content.Server.Zombies;

public sealed class MakeZombieSystem : EntitySystem
{
    [Dependency] private readonly ZombifyOnDeathSystem _zombie = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MakeZombieComponent, MapInitEvent>(OnSpawnZombie);
        SubscribeLocalEvent<MakeZombieComponent, GhostRoleSpawnerUsedEvent>(OnGhostSpawnZombie);
    }

    private void OnGhostSpawnZombie(EntityUid uid, MakeZombieComponent component, GhostRoleSpawnerUsedEvent args)
    {
        // You're alive! Now you're undead! (spawner version)
        RemCompDeferred <MakeZombieComponent>(uid);
        _zombie.ZombifyEntity(uid);
    }

    private void OnSpawnZombie(EntityUid uid, MakeZombieComponent component, MapInitEvent args)
    {
        // You're alive! Now you're undead!
        RemCompDeferred <MakeZombieComponent>(uid);
        _zombie.ZombifyEntity(uid);
    }
}
