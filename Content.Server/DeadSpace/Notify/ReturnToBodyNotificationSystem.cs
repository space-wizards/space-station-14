using Content.Server.EUI;
using Content.Server.Ghost;
using Content.Shared.DeadSpace.Necromorphs.InfectionDead;
using Content.Shared.DeadSpace.Necromorphs.InfectionDead.Components;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Zombies;
using Robust.Shared.Player;

namespace Content.Server.DeadSpace.Notify;

/// <summary>
/// Notifies ghosting players when their original body becomes available again.
/// </summary>
public sealed class ReturnToBodyNotificationSystem : EntitySystem
{
    [Dependency] private readonly EuiManager _eui = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<EntityZombifiedEvent>(OnZombified);
        SubscribeLocalEvent<NecromorfComponent, NecroficationStartedEvent>(OnNecroficationStarted);
    }

    private void OnMobStateChanged(MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Alive ||
            args.OldMobState is not (MobState.Critical or MobState.Dead) ||
            HasComp<ZombieComponent>(args.Target) ||
            HasComp<NecromorfComponent>(args.Target))
        {
            return;
        }

        Notify(args.Target);
    }

    private void OnZombified(ref EntityZombifiedEvent args)
    {
        Notify(args.Target);
    }

    private void OnNecroficationStarted(Entity<NecromorfComponent> ent, ref NecroficationStartedEvent args)
    {
        Notify(ent.Owner);
    }

    private void Notify(EntityUid target)
    {
        if (!_mind.TryGetMind(target, out _, out var mind) ||
            mind.CurrentEntity == target ||
            mind.UserId is not { } userId ||
            !_player.TryGetSessionById(userId, out var session))
        {
            return;
        }

        _eui.OpenEui(new ReturnToBodyEui(mind, _mind, _player), session);
    }
}
