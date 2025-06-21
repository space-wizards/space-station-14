// these are HEAVILY based on the Bingle free-agent ghostrole from GoobStation, but reflavored and reprogrammed to make them more Robust (and less of a meme.)
// all credit for the core gameplay concepts and a lot of the core functionality of the code goes to the folks over at Goob, but I re-wrote enough of it to justify putting it in our filestructure.
// the original Bingle PR can be found here: https://github.com/Goob-Station/Goob-Station/pull/1519

using System.Linq;
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
using Content.Shared.Throwing;
using Robust.Shared.Prototypes;
using Content.Shared.Stacks;

namespace Content.Shared._Impstation.Replicator;

public abstract class SharedReplicatorNestSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
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
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReplicatorNestComponent, StepTriggeredOffEvent>(OnStepTriggered);

        SubscribeLocalEvent<ReplicatorComponent, ReplicatorUpgradeActionEvent>(OnUpgrade);
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

        // *reject* if blacklisted
        if (_whitelist.IsBlacklistPass(ent.Comp.Blacklist, args.Tripper))
        {
            if (TryComp<PullableComponent>(args.Tripper, out var pullable) && pullable.BeingPulled)
                _pulling.TryStopPull(args.Tripper, pullable);

            var xform = Transform(ent);
            var xformQuery = GetEntityQuery<TransformComponent>();
            var worldPos = _xform.GetWorldPosition(xform, xformQuery);

            var direction = _xform.GetWorldPosition(args.Tripper, xformQuery) - worldPos;
            _throwing.TryThrow(args.Tripper, direction * 10, 7, ent, 0);
            return;
        }

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
        // regardless of what falls in, you get at least one point
        if (!HasComp<StackComponent>(tripper)) // as long as it's not a stack.
        {
            ent.Comp.TotalPoints += 10;
            ent.Comp.SpawningProgress += 10;
        }

        // if the item is in a stack, you get points depending on how many items are in that stack.
        if (TryComp<StackComponent>(tripper, out var stackComp))
        {
            ent.Comp.TotalPoints += stackComp.Count;
            ent.Comp.SpawningProgress += stackComp.Count;
        }

        // you get a bonus point if the item is Large, 2 bonus points if it's Huge, and 3 bonus points if it's above that.
        else if (TryComp<ItemComponent>(tripper, out var itemComp))
        {
            if (_item.GetSizePrototype(itemComp.Size) == _item.GetSizePrototype("Large"))
                ent.Comp.TotalPoints += 10;
            else if (_item.GetSizePrototype(itemComp.Size) == _item.GetSizePrototype("Huge"))
                ent.Comp.TotalPoints += 20;
            else if (_item.GetSizePrototype(itemComp.Size) >= _item.GetSizePrototype("Ginormous"))
                ent.Comp.TotalPoints += 30;
            // regardless, items only net 1 spawning progress.
            ent.Comp.SpawningProgress += 10;
        }

        // if it wasn't an item and was anchorable, you get 3 bonus points.
        else if (TryComp<AnchorableComponent>(tripper, out _))
        {
            ent.Comp.TotalPoints += 30;
            // structures give a lot more spawning progress than items
            ent.Comp.SpawningProgress += 30;
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

            // update the threshold for the next upgrade (the default times the current level), and upgrade all our guys.
            // threshold increases plateau at the endgame level.
            ent.Comp.NextUpgradeAt += ent.Comp.CurrentLevel >= ent.Comp.EndgameLevel ? ent.Comp.UpgradeAt * ent.Comp.EndgameLevel : ent.Comp.UpgradeAt * ent.Comp.CurrentLevel;
            UpgradeAll(ent);
            _audio.PlayPvs(ent.Comp.LevelUpSound, ent);
        }

        // after upgrading, if we exceed the next spawn threshold, spawn a new (un-upgraded) replicator, then set the next spawn threshold.
        if (ent.Comp.SpawningProgress >= ent.Comp.NextSpawnAt)
        {
            SpawnNew(ent);
            ent.Comp.NextSpawnAt += ent.Comp.SpawnNewAt * ent.Comp.UnclaimedSpawners.Count;
        }

        // and dirty so the client knows if it's supposed to update the nest visuals
        Dirty(ent);

        // finally, update the PointsStorage entity.
        if (!TryComp<ReplicatorNestPointsStorageComponent>(ent.Comp.PointsStorage, out var pointsStorageComponent))
            pointsStorageComponent = EnsureComp<ReplicatorNestPointsStorageComponent>(ent.Comp.PointsStorage);

        pointsStorageComponent.Level = ent.Comp.CurrentLevel;
        pointsStorageComponent.TotalPoints = ent.Comp.TotalPoints;
        pointsStorageComponent.TotalReplicators = ent.Comp.SpawnedMinions.Count;
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
            if (!TryComp<ReplicatorComponent>(replicator, out var comp) || comp.UpgradeActions.Count == 0)
                continue;

            if (comp.UpgradeStage >= ent.Comp.MaxUpgradeStage || comp.HasBeenGivenUpgradeActions == true)
                continue;

            if (!TryComp<MindContainerComponent>(replicator, out var mindContainer) || mindContainer.Mind == null)
                continue;

            foreach (var action in comp.UpgradeActions)
            {
                if (!mindContainer.HasMind)
                    comp.Actions.Add(_actions.AddAction(replicator, action));
                else if (mindContainer.Mind != null)
                    comp.Actions.Add(_actionContainer.AddAction((EntityUid)mindContainer.Mind, action));
            }
            comp.HasBeenGivenUpgradeActions = true;
        }
    }

    public void OnUpgrade(Entity<ReplicatorComponent> ent, ref ReplicatorUpgradeActionEvent args)
    {
        // don't run this clientside
        if (_net.IsClient || !_timing.IsFirstTimePredicted)
            return;

        if (ent.Comp.MyNest == null || UpgradeReplicator(ent, ref args) == null)
        {
            _popup.PopupEntity(Loc.GetString("replicator-cant-find-nest"), ent, PopupType.MediumCaution);
            return;
        }

        QueueDel(ent);
        foreach (var action in ent.Comp.Actions)
        {
            QueueDel(action);
        }

        _popup.PopupEntity(Loc.GetString($"{ent.Comp.ReadyToUpgradeMessage}-others"), ent, PopupType.MediumCaution);
    }

    public EntityUid? UpgradeReplicator(Entity<ReplicatorComponent> ent, ref ReplicatorUpgradeActionEvent args)
    {
        if (!_mind.TryGetMind(ent, out var mind, out _))
            return null;

        var xform = Transform(ent);
        var nextStage = args.NextStage;

        var upgraded = Spawn(nextStage, xform.Coordinates);
        var upgradedComp = EnsureComp<ReplicatorComponent>(upgraded);
        upgradedComp.RelatedReplicators = ent.Comp.RelatedReplicators;
        upgradedComp.MyNest = ent.Comp.MyNest;

        if (ent.Comp.MyNest != null)
        {
            var nestComp = EnsureComp<ReplicatorNestComponent>((EntityUid)ent.Comp.MyNest);
            nestComp.SpawnedMinions.Remove(ent);
            nestComp.SpawnedMinions.Add(upgraded);
        }

        _mind.TransferTo(mind, upgraded);

        _popup.PopupEntity(Loc.GetString($"{ent.Comp.ReadyToUpgradeMessage}-self"), ent, PopupType.Medium); //this seems to not work with my changes? - ruddygreat

        return upgraded;
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

public sealed partial class ReplicatorUpgradeActionEvent : InstantActionEvent
{
    [DataField(required: true)]
    public EntProtoId NextStage;
}

[ByRefEvent]
public sealed partial class ReplicatorNestEmbiggenedEvent(Entity<ReplicatorNestComponent> ent) : EntityEventArgs
{
    public Entity<ReplicatorNestComponent> Ent { get; set; } = ent;
}
