using Content.Shared.Popups;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Zombies;

/// <summary>
///   Manages the still-human "patient0" players who will turn into zombies when they choose to do unless we force
///   them first. As soon as they start turning into zombies they gain PendingZombieComponent and no longer have
///   InitialInfectedComponent.
/// </summary>
public abstract class SharedInitialInfectedSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] public readonly SharedPopupSystem Popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InitialInfectedComponent, EntityUnpausedEvent>(OnUnpause);
    }

    // Hurt them each second. Once they die, PendingZombieSystem will call Zombify and remove InitialInfectedComponent
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var curTime = Timing.CurTime;

        // Update all initial infected
        var query = EntityQueryEnumerator<InitialInfectedComponent, ZombieComponent>();
        while (query.MoveNext(out var initialUid, out var initial, out var zombie))
        {
            if (initial.TurnForced < curTime)
            {
                ForceInfection(initialUid, zombie);
            }
        }
    }

    public void ForceInfection(EntityUid uid, ZombieComponent? zombie = null)
    {
        if (!Resolve(uid, ref zombie))
            return;

        // Time to begin forcing this initial infected to turn.
        var pending = EnsureComp<PendingZombieComponent>(uid);
        pending.GracePeriod =
            _random.NextFloat(0.25f, 1.0f) * zombie.Settings.InfectionTurnTime;
        pending.InfectionStarted = Timing.CurTime;
        pending.VirusDamage = zombie.Settings.VirusDamage;

        RemCompDeferred<InitialInfectedComponent>(uid);

        Popup.PopupEntity(Loc.GetString("zombie-forced"), uid, uid);
    }

    private void OnUnpause(EntityUid uid, InitialInfectedComponent component, ref EntityUnpausedEvent args)
    {
        component.FirstTurnAllowed += args.PausedTime;
        component.TurnForced += args.PausedTime;
    }

}
