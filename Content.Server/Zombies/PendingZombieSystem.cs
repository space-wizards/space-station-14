using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Zombies;

namespace Content.Server.Zombies;

public class PendingZombieSystem : SharedPendingZombieSystem
{
    [Dependency] private readonly ZombieSystem _zombie = default!;

    public override void Initialize()
    {
        base.Initialize();

    }

    protected virtual void ZombifyNow(EntityUid uid, PendingZombieComponent pending, ZombieComponent zombie, MobStateComponent mobState)
    {
        // NB: This removes PendingZombieComponent
        _zombie.ZombifyEntity(uid, pending: pending, mobState: mobState);
    }

}
