using Content.Server.Actions;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Zombies;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Zombies;

/// <summary>
///   Manages the still-human "patient0" players who will turn into zombies when they choose to do unless we force
///   them first. As soon as they start turning into zombies they gain PendingZombieComponent and no longer have
///   InitialInfectedComponent.
/// </summary>
public sealed class InitialInfectedSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ZombieSystem _zombie = default!;
    [Dependency] private readonly ActionsSystem _action = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] public readonly SharedPopupSystem Popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ZombieComponent, ZombifySelfActionEvent>(OnZombifySelf);
        SubscribeLocalEvent<InitialInfectedComponent, MobStateChangedEvent>(OnMobState);
        SubscribeLocalEvent<InitialInfectedComponent, EntityUnpausedEvent>(OnUnpause);
    }

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
        _zombie.Infect(uid, _random.NextFloat(0.25f, 1.0f) * zombie.InfectionTurnTime, zombie.DeadMinTurnTime);

        RemCompDeferred<InitialInfectedComponent>(uid);

        Popup.PopupEntity(Loc.GetString("zombie-forced"), uid, uid);
    }

    private void OnUnpause(EntityUid uid, InitialInfectedComponent component, ref EntityUnpausedEvent args)
    {
        component.FirstTurnAllowed += args.PausedTime;
        component.TurnForced += args.PausedTime;
    }

    // If they crit or die while Initially infected, switch to an active virus.
    private void OnMobState(EntityUid uid, InitialInfectedComponent initial, MobStateChangedEvent args)
    {
        // Check if too soon to become a zombie
        if (initial.FirstTurnAllowed > Timing.CurTime)
            return;

        if (args.NewMobState == MobState.Dead)
        {
            // Zombify them immediately
            _zombie.ZombifyEntity(uid);
        }
        else if (args.NewMobState == MobState.Critical)
        {
            // Immediately jump to an active virus now
            ForceInfection(uid);
        }
    }

    // Both allowed and forced should be times relative to curTime.
    public void AddInitialInfecton(EntityUid uid, TimeSpan allowed, TimeSpan forced, ZombieComponent? ruleSettings)
    {
        var initial = EnsureComp<InitialInfectedComponent>(uid);
        initial.FirstTurnAllowed = allowed;
        // Only take damage after this many seconds
        initial.TurnForced = forced;

        var curTime = Timing.CurTime;
        var action = new InstantAction(_prototypeManager.Index<InstantActionPrototype>(ZombieRuleComponent.ZombifySelfActionPrototype));

        // Set a cooldown on the action here that reflects the time until initial infection.
        if (allowed > curTime)
            action.Cooldown = (curTime, allowed);
        _action.AddAction(uid, action, null);

        _zombie.AddZombieTo(uid, ruleSettings);
    }

    private void OnZombifySelf(EntityUid uid, ZombieComponent zombie, ZombifySelfActionEvent args)
    {
        // It is possible for an Initial Infected to be forced to zombify (by time or infection count) and enter
        //   this function after InitialInfectedComponent has been removed. But if it is still here, check the time.
        if (TryComp<InitialInfectedComponent>(uid, out var initial))
        {
            // Check it's not too early to zombify
            // (note SharedInitialInfectedSystem manages FirstTurnAllowed on unpause)
            if (initial.FirstTurnAllowed > Timing.CurTime)
                return;

            _zombie.ZombifyEntity(uid, zombie:zombie);
        }
        else if (HasComp<PendingZombieComponent>(uid))
        {
            // If not initial, they must at least have a pending virus to become a zombie
            _zombie.ZombifyEntity(uid, zombie:zombie);
        }

        var action = new InstantAction(_prototypeManager.Index<InstantActionPrototype>(ZombieRuleComponent.ZombifySelfActionPrototype));
        _action.RemoveAction(uid, action);
    }

    public void ActivateZombifyOnDeath()
    {
        var query = EntityQueryEnumerator<InitialInfectedComponent, ZombieComponent, MobStateComponent>();
        while (query.MoveNext(out var uid, out var initial, out var zombie, out var mobState))
        {
            // This is probably already in the past, but just in case the rule has had a time shortened, set it now.
            initial.FirstTurnAllowed = TimeSpan.Zero;

            if (mobState.CurrentState == MobState.Dead)
            {
                // Zombify them immediately
                _zombie.ZombifyEntity(uid, mobState, zombie:zombie);
            }
            else if (mobState.CurrentState == MobState.Critical)
            {
                // Immediately jump to an active virus now
                ForceInfection(uid, zombie);
            }
            else
            {
                Popup.PopupEntity(Loc.GetString("zombie-infection-ready"), uid, uid);
            }
        }
    }
}
