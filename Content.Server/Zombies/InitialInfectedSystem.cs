using Content.Server.Actions;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Zombies;
using Robust.Shared.Prototypes;

namespace Content.Server.Zombies;

public sealed class InitialInfectedSystem : SharedInitialInfectedSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ZombieSystem _zombie = default!;
    [Dependency] private readonly ActionsSystem _action = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ZombieComponent, ZombifySelfActionEvent>(OnZombifySelf);
        SubscribeLocalEvent<InitialInfectedComponent, MobStateChangedEvent>(OnMobState);
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
    public void AddInitialInfecton(EntityUid uid, TimeSpan allowed, TimeSpan forced)
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
    }

    // Force every existing initial infected in this rule to turn very soon.
    //
    // Some players were "forgetting" that they were initial infected and playing most or all of the round
    // as players, even after zombies had rampaged across the entire ship. This ensures that as the horde takes
    // hold, all possible zombies convert.
    public void ForceZombies(EntityUid ruleUid, ZombieRuleComponent zombies)
    {
        var pendingQuery = EntityQueryEnumerator<InitialInfectedComponent, ZombieComponent>();
        while (pendingQuery.MoveNext(out var uid, out var initial, out var zombie))
        {
            if (zombie.Family.Rules == ruleUid)
            {
                // Immediately jump to an active virus for initial players
                ForceInfection(uid, zombie);
            }
        }
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

    public void ActivateZombifyOnDeath(EntityUid ruleUid, ZombieRuleComponent component)
    {
        var query = EntityQueryEnumerator<InitialInfectedComponent, ZombieComponent, MobStateComponent>();
        while (query.MoveNext(out var uid, out var initial, out var zombie, out var mobState))
        {
            // Don't change zombies that don't belong to these rules.
            if (zombie.Family.Rules != ruleUid)
                continue;

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
