// these are HEAVILY based on the Bingle free-agent ghostrole from GoobStation, but reflavored and reprogrammed to make them more Robust (and less of a meme.)
// all credit for the core gameplay concepts and a lot of the core functionality of the code goes to the folks over at Goob, but I re-wrote enough of it to justify putting it in our filestructure.
// the original Bingle PR can be found here: https://github.com/Goob-Station/Goob-Station/pull/1519

using Content.Shared._Impstation.SpawnedFromTracker;
using Content.Shared.Actions;
using Content.Shared.Construction.Components;
using Content.Shared.Humanoid;
using Content.Shared.Item;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Popups;
using Content.Shared.StepTrigger.Systems;
using Content.Shared.Stunnable;
using Content.Shared.Whitelist;
using Content.Shared.Mind.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._Impstation.Replicator;

public abstract class SharedReplicatorNestSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;

    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly StepTriggerSystem _stepTrigger = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReplicatorNestComponent, StepTriggeredOffEvent>(OnStepTriggered);

        SubscribeLocalEvent<ReplicatorComponent, ReplicatorUpgrade2ActionEvent>(OnUpgrade2);
        SubscribeLocalEvent<ReplicatorComponent, ReplicatorUpgrade3ActionEvent>(OnUpgrade3);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_net.IsClient)
            return;

        // this is jank but i need to do it to communicate this information over to the client
        var query = EntityQueryEnumerator<ReplicatorNestComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.NeedsUpdate)
            {
                Embiggen((uid, comp));
                comp.NeedsUpdate = false;
            }
        }
    }

    private void OnStepTriggered(Entity<ReplicatorNestComponent> ent, ref StepTriggeredOffEvent args)
    {
        // dont accept if they are already falling
        if (HasComp<ReplicatorNestFallingComponent>(args.Tripper))
            return;

        var isReplicator = HasComp<ReplicatorComponent>(args.Tripper);

        // Allow dead replicators regardless of current level. 
        if (TryComp<MobStateComponent>(args.Tripper, out var mobState) && isReplicator && _mobState.IsDead(args.Tripper))
        {
            StartFalling(ent, args.Tripper);
            return;
        }

        // Don't allow living beings. If you want those sweet bonus points, you have to kill.
        if (mobState != null && _mobState.IsAlive(args.Tripper))
            return;

        StartFalling(ent, args.Tripper);
    }

    private void StartFalling(Entity<ReplicatorNestComponent> ent, EntityUid tripper, bool playSound = true)
    {
        HandlePoints(ent, tripper);

        if (TryComp<PullableComponent>(tripper, out var pullable) && pullable.BeingPulled)
            _pulling.TryStopPull(tripper, pullable);

        // handle starting the falling animation
        var fall = EnsureComp<ReplicatorNestFallingComponent>(tripper);
        fall.FallingTarget = ent;
        fall.NextDeletionTime = _timing.CurTime + fall.DeletionTime;
        // no funny business
        _stun.TryKnockdown(tripper, fall.DeletionTime, false);

        if (playSound)
            _audio.PlayPvs(ent.Comp.FallingSound, tripper);
    }

    private void HandlePoints(Entity<ReplicatorNestComponent> ent, EntityUid tripper) // this is its own method because I think it reads cleaner. also the way goobcode handled this sucked.
    {
        if (_whitelist.IsBlacklistPass(ent.Comp.Blacklist, tripper))
            return;

        // regardless of what falls in, you get at least one point.
        ent.Comp.TotalPoints++;
        ent.Comp.SpawningProgress++;

        // you get a bonus point if the item is Large, 2 bonus points if it's Huge, and 3 bonus points if it's above that.
        if (TryComp<ItemComponent>(tripper, out var itemComp))
        {
            if (_item.GetSizePrototype(itemComp.Size) == _item.GetSizePrototype("Large"))
                ent.Comp.TotalPoints++;
            else if (_item.GetSizePrototype(itemComp.Size) == _item.GetSizePrototype("Huge"))
                ent.Comp.TotalPoints += 2;
            else if (_item.GetSizePrototype(itemComp.Size) >= _item.GetSizePrototype("Ginormous"))
                ent.Comp.TotalPoints += 3;
            // regardless, items only net 1 spawning progress.
            ent.Comp.SpawningProgress++;
        }

        // if it wasn't an item and was anchorable, you get 3 bonus points.
        else if (TryComp<AnchorableComponent>(tripper, out _))
        {
            ent.Comp.TotalPoints += 3;
            // structures give a lot more spawning progress than items
            ent.Comp.SpawningProgress += 3;
        }

        // recycling four dead replicators nets you one new replicator, but no progress towards leveling up.
        else if (HasComp<ReplicatorComponent>(tripper))
            ent.Comp.SpawningProgress += ent.Comp.SpawnNewAt / 4;

        // now we handle points if it *isn't* a replicator, structure, or item, but *is* a living thing
        else if (HasComp<MobStateComponent>(tripper))
        {
            // you get additional bonus points if it was a humanoid:
            if (HasComp<HumanoidAppearanceComponent>(tripper))
            {
                // bonus points for humanoid (default 2) times current level
                ent.Comp.TotalPoints += ent.Comp.BonusPointsHumanoid * ent.Comp.CurrentLevel;
                // plus you get enough progress for one new replicator
                ent.Comp.SpawningProgress += ent.Comp.SpawnNewAt;
            }
            // otherwise, you get bonus points for living (default 1) times current level
            else
            {
                ent.Comp.TotalPoints += ent.Comp.BonusPointsAlive * ent.Comp.CurrentLevel;
                // and 1/4th progress
                ent.Comp.SpawningProgress += ent.Comp.SpawnNewAt / 4;
            }
        }

        // if we exceed the upgrade threshold after points are added, 
        if (ent.Comp.TotalPoints >= ent.Comp.NextUpgradeAt)
        {
            // level up
            ent.Comp.CurrentLevel++;

            // this allows us to have an arbitrary number of unique messages for when the nest levels up - and a default for if we run out. 
            var growthMessage = $"replicator-nest-level{ent.Comp.CurrentLevel}";
            if (Loc.TryGetString(growthMessage, out var localizedMsg))
                _popup.PopupEntity(localizedMsg, ent);
            else
                _popup.PopupEntity(Loc.GetString("replicator-nest-levelup"), ent);

            // make the nest sprite grow as long as we have sprites for it. I am NOT scaling it.
            if (ent.Comp.CurrentLevel <= ent.Comp.EndgameLevel)
                ent.Comp.NeedsUpdate = true;

            // if we've reached the endgame, the nest will ignore gravity when picking targets - actively pulling them in.
            if (ent.Comp.CurrentLevel == ent.Comp.EndgameLevel)
                _stepTrigger.SetIgnoreWeightless(ent, true);

            // update the threshold for the next upgrade (the default times the current level), and upgrade all our guys.
            // threshold increases plateau at the endgame level.
            ent.Comp.NextUpgradeAt += ent.Comp.CurrentLevel >= ent.Comp.EndgameLevel ? ent.Comp.UpgradeAt * ent.Comp.EndgameLevel : ent.Comp.UpgradeAt * ent.Comp.CurrentLevel;
            UpgradeAll(ent);
        }

        // after upgrading, if we exceed the next spawn threshold, spawn a new (un-upgraded) replicator, then set the next spawn threshold.
        if (ent.Comp.SpawningProgress >= ent.Comp.NextSpawnAt)
        {
            SpawnNew(ent);
            ent.Comp.NextSpawnAt += ent.Comp.SpawnNewAt;
        }

        // and dirty so the client knows if it's supposed to update the nest visuals
        Dirty(ent);
    }

    private void SpawnNew(Entity<ReplicatorNestComponent> ent)
    {
        // SUPER don't run this clientside
        if (_net.IsClient)
            return;

        // spawn a new replicator
        var spawner = Spawn(ent.Comp.ToSpawn, Transform(ent).Coordinates);
        // TODO:
        //OnSpawnTile(ent, ent.comp.Level * 2, "FloorReplicator");

        // make sure our new GhostRoleSpawnPoint knows where it came from, so it can pass that down to the replicator it spawns.
        var tracker = EnsureComp<SpawnedFromTrackerComponent>(spawner);
        tracker.SpawnedFrom = ent;

        ent.Comp.UnclaimedSpawners.Add(spawner);
    }

    public void UpgradeAll(Entity<ReplicatorNestComponent> ent)
    {
        // don't run this clientside
        if (_net.IsClient || !_timing.IsFirstTimePredicted)
            return;

        foreach (var replicator in ent.Comp.SpawnedMinions)
        {
            if (!TryComp<ReplicatorComponent>(replicator, out var comp))
                continue;

            if (comp.UpgradeStage >= ent.Comp.MaxUpgradeStage || comp.TargetUpgradeStage >= ent.Comp.MaxUpgradeStage)
                continue;

            if (!TryComp<MindContainerComponent>(replicator, out var mindContainer) || mindContainer.Mind == null)
                continue;

            comp.TargetUpgradeStage++;

            var targetAction = comp.TargetUpgradeStage == 1 ? comp.Level2Action : comp.Level3Action;

            if (!mindContainer.HasMind)
                _actions.AddAction(replicator, targetAction);
            else if (mindContainer.Mind != null)
                _actionContainer.AddAction((EntityUid)mindContainer.Mind, targetAction);
        }
    }

    public void OnUpgrade2(Entity<ReplicatorComponent> ent, ref ReplicatorUpgrade2ActionEvent args)
    {
        // don't run this clientside
        if (_net.IsClient || !_timing.IsFirstTimePredicted)
            return;

        UpgradeReplicator(ent, 2);

        QueueDel(ent);
        QueueDel(args.Action);
    }

    public void OnUpgrade3(Entity<ReplicatorComponent> ent, ref ReplicatorUpgrade3ActionEvent args)
    {
        // don't run this clientside
        if (_net.IsClient || !_timing.IsFirstTimePredicted)
            return;

        UpgradeReplicator(ent, 3);

        QueueDel(ent);
        QueueDel(args.Action);
    }

    public void UpgradeReplicator(Entity<ReplicatorComponent> ent, int desiredLevel)
    {
        var xform = Transform(ent);

        // if adding more stages, maybe make this a switch.
        var nextStage = desiredLevel == 2 ? ent.Comp.Level2Id : ent.Comp.Level3Id;

        var upgraded = Spawn(nextStage, xform.Coordinates);
        var upgradedComp = EnsureComp<ReplicatorComponent>(upgraded);
        upgradedComp.RelatedReplicators = ent.Comp.RelatedReplicators;
        upgradedComp.TargetUpgradeStage = ent.Comp.TargetUpgradeStage;

        if (ent.Comp.MyNest != null)
        {
            var nestComp = EnsureComp<ReplicatorNestComponent>((EntityUid)ent.Comp.MyNest);
            nestComp.SpawnedMinions.Remove(ent);
            nestComp.SpawnedMinions.Add(upgraded);
        }

        if (!_mind.TryGetMind(ent, out var mind, out _))
            return;

        _mind.TransferTo(mind, upgraded);

        return;
    }

    private void Embiggen(Entity<ReplicatorNestComponent> ent)
    {
        var ev = new ReplicatorNestEmbiggenedEvent(ent);
        RaiseLocalEvent(ent, ref ev);
    }
}

public sealed partial class ReplicatorSpawnNestActionEvent : InstantActionEvent
{

}

public sealed partial class ReplicatorUpgrade2ActionEvent : InstantActionEvent
{

}

public sealed partial class ReplicatorUpgrade3ActionEvent : InstantActionEvent
{

}

[ByRefEvent]
public sealed partial class ReplicatorNestEmbiggenedEvent(Entity<ReplicatorNestComponent> ent) : EntityEventArgs
{
    public Entity<ReplicatorNestComponent> Ent { get; set; } = ent;
}
