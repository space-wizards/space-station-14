using Content.Server.Ghost.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Zombies;

namespace Content.Server.Zombies;

public class PendingZombieSystem : SharedPendingZombieSystem
{
    [Dependency] private readonly ZombieSystem _zombie = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PendingZombieComponent, GhostMoveAttempt>(OnAttemptGhost);
    }

    private void OnAttemptGhost(EntityUid uid, PendingZombieComponent component, GhostMoveAttempt args)
    {
        // Don't allow ghosting through movement while waiting to zombify
        args.Cancel();
    }

    protected override void ZombifyNow(EntityUid uid, PendingZombieComponent pending, ZombieComponent zombie, MobStateComponent mobState)
    {
        // NB: This removes PendingZombieComponent
        _zombie.ZombifyEntity(uid, mobState: mobState, zombie:zombie);
    }

}
