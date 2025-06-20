// these are HEAVILY based on the Bingle free-agent ghostrole from GoobStation, but reflavored and reprogrammed to make them more Robust (and less of a meme.)
// all credit for the core gameplay concepts and a lot of the core functionality of the code goes to the folks over at Goob, but I re-wrote enough of it to justify putting it in our filestructure.
// the original Bingle PR can be found here: https://github.com/Goob-Station/Goob-Station/pull/1519

using Content.Server._Impstation.Administration.Components;
using Content.Server.Actions;
using Content.Server.Audio;
using Content.Server.GameTicking;
using Content.Server.Pinpointer;
using Content.Server.Popups;
using Content.Server.Stunnable;
using Content.Shared._Impstation.Replicator;
using Content.Shared.Actions;
using Content.Shared.Audio;
using Content.Shared.Destructible;
using Content.Shared.Explosion.Components;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Mech.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Events;
using Content.Shared.Objectives.Components;
using Content.Shared.Pinpointer;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.StepTrigger.Systems;
using Content.Shared.Stunnable;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Server._Impstation.Replicator;

public sealed class ReplicatorNestSystem : SharedReplicatorNestSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly ContainerSystem _containerSystem = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly StunSystem _stun = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly PinpointerSystem _pinpointer = default!;
    [Dependency] private readonly AmbientSoundSystem _ambientSound = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReplicatorNestComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ReplicatorNestComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
        SubscribeLocalEvent<ReplicatorNestComponent, StepTriggerAttemptEvent>(OnStepTriggerAttempt);
        SubscribeLocalEvent<ReplicatorNestFallingComponent, UpdateCanMoveEvent>(OnUpdateCanMove);
        SubscribeLocalEvent<ReplicatorNestComponent, DestructionEventArgs>(OnDestroyed);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndTextAppend);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        HashSet<EntityUid> toDel = [];

        var query = EntityQueryEnumerator<ReplicatorNestFallingComponent>();
        while (query.MoveNext(out var uid, out var falling))
        {
            if (_timing.CurTime < falling.NextDeletionTime)
                continue;

            if (HasComp<SiliconLawBoundComponent>(uid) || // Delete borgs
                !HasComp<HumanoidAppearanceComponent>(uid) && // Store people
                !HasComp<OnUseTimerTriggerComponent>(uid) && // Store grenades
                !HasComp<StealTargetComponent>(uid) && // Store steal targets
                !HasComp<MechComponent>(uid) && // Store mechs like Ripley
                !TryComp<MindContainerComponent>(uid, out var mind) | (mind != null && !mind!.HasMind)) // Store anything else that has a mind
            {
                toDel.Add(uid);
            }

            _containerSystem.Insert(uid, falling.FallingTarget.Comp.Hole);
            EnsureComp<StunnedComponent>(uid); // used stunned to prevent any funny being done inside the pit
            RemCompDeferred(uid, falling);
        }

        foreach (var uid in toDel)
        {
            QueueDel(uid);
        }
    }

    private void OnEntRemoved(Entity<ReplicatorNestComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        RemCompDeferred<StunnedComponent>(args.Entity);
    }

    private void OnMapInit(Entity<ReplicatorNestComponent> ent, ref MapInitEvent args)
    {
        if (!Transform(ent).Coordinates.IsValid(EntityManager))
            QueueDel(ent);

        ent.Comp.Hole = _containerSystem.EnsureContainer<Container>(ent, "hole");

        ent.Comp.NextSpawnAt = ent.Comp.SpawnNewAt;
        ent.Comp.NextUpgradeAt = ent.Comp.UpgradeAt;

        var pointsStorageEnt = Spawn("ReplicatorNestPointsStorage", Transform(ent).Coordinates);
        EnsureComp<ReplicatorNestPointsStorageComponent>(pointsStorageEnt);

        ent.Comp.PointsStorage = pointsStorageEnt;
    }

    private void OnStepTriggerAttempt(Entity<ReplicatorNestComponent> ent, ref StepTriggerAttemptEvent args)
    {
        args.Continue = true;
    }

    private void OnUpdateCanMove(Entity<ReplicatorNestFallingComponent> ent, ref UpdateCanMoveEvent args)
    {
        args.Cancel();
    }

    private void OnDestroyed(Entity<ReplicatorNestComponent> ent, ref DestructionEventArgs args)
    {
        HandleDestruction(ent);
    }

    private void HandleDestruction(Entity<ReplicatorNestComponent> ent)
    {
        if (ent.Comp.Hole != null)
        {
            foreach (var uid in _containerSystem.EmptyContainer(ent.Comp.Hole))
            {
                RemCompDeferred<StunnedComponent>(uid);
                _stun.TryKnockdown(uid, TimeSpan.FromSeconds(2), false);
            }
        }

        // delete all unclaimed spawners
        foreach (var spawner in ent.Comp.UnclaimedSpawners)
        {
            ent.Comp.UnclaimedSpawners.Remove(spawner);
            QueueDel(spawner);
        }

        // remove the falling component from anyone currently falling into this nest
        var query = EntityQueryEnumerator<ReplicatorNestFallingComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.FallingTarget == ent)
                RemCompDeferred<ReplicatorNestFallingComponent>(uid);
        }

        // Figure out who the queen is & which replicators belonging to this nest are still alive.
        EntityUid? queen = null;
        HashSet<Entity<ReplicatorComponent>> livingReplicators = [];
        foreach (var replicator in ent.Comp.SpawnedMinions)
        {
            if (!TryComp<ReplicatorComponent>(replicator, out var replicatorComp))
                continue;

            if (!_mobState.IsAlive(replicator))
                continue;

            replicatorComp.MyNest = null;

            if (replicatorComp.Queen)
                queen = replicator;

            livingReplicators.Add((replicator, replicatorComp));

            _popup.PopupEntity(Loc.GetString("replicator-nest-destroyed"), replicator, replicator, Shared.Popups.PopupType.LargeCaution);
        }

        // if there are living replicators, select one and give the action to create a new nest.
        if (livingReplicators.Count > 0)
        {
            // if there's no queen, pick a new one
            if (queen == null)
                queen = _random.Pick(livingReplicators);

            var comp = EnsureComp<ReplicatorComponent>((EntityUid)queen);
            comp.Queen = true;
            livingReplicators.Add(((EntityUid)queen, comp));
            comp.RelatedReplicators = livingReplicators; // make sure we know who belongs to our nest

            if (!TryComp<MindContainerComponent>(queen, out var mindContainer) || mindContainer.Mind == null)
                return;

            if (!mindContainer.HasMind)
                comp.Actions.Add(_actions.AddAction((EntityUid)queen, ent.Comp.SpawnNewNestAction));
            else
                comp.Actions.Add(_actionContainer.AddAction((EntityUid)mindContainer.Mind, ent.Comp.SpawnNewNestAction));

            // then add the Crown.
            EnsureComp<ReplicatorSignComponent>((EntityUid)queen);
        }

        // finally, loop over our living replicators and set their pinpointers to target the queen.
        foreach (var replicator in livingReplicators)
        {
            if (!_inventory.TryGetSlotEntity(replicator, "pocket1", out var pocket1) || !TryComp<PinpointerComponent>(pocket1, out var pinpointer))
                continue;
            // set the target to the queen
            _pinpointer.SetTarget(pocket1.Value, queen, pinpointer);
        }

        // turn off the ambient sound on the points storage entity.
        if (TryComp<AmbientSoundComponent>(ent.Comp.PointsStorage, out var ambientComp))
            _ambientSound.SetAmbience(ent.Comp.PointsStorage, false, ambientComp);
    }

    private void OnRoundEndTextAppend(RoundEndTextAppendEvent args)
    {
        List<Entity<ReplicatorNestPointsStorageComponent>> nests = [];

        // get all the nests that have existed this round in a list
        var query = AllEntityQuery<ReplicatorNestPointsStorageComponent>();
        while (query.MoveNext(out var uid, out var comp))
            nests.Add((uid, comp));

        if (nests.Count == 0)
            return;

        // linebreak
        args.AddLine("");

        var totalPoints = 0;
        var totalSpawned = 0;
        HashSet<int> levels = [];
        var locationsList = "";

        // generate a summary of locations, levels, points, and total spawned replicators across all nests
        var i = 0;
        foreach (var ent in nests)
        {
            i++;
            var pointsStorage = ent.Comp;
            var location = "Unknown";
            var mapCoords = _transform.ToMapCoordinates(Transform(ent).Coordinates);
            if (_navMap.TryGetNearestBeacon(mapCoords, out var beacon, out _) && beacon?.Comp.Text != null)
                location = beacon?.Comp.Text!;

            if (nests.Count == 1)
                locationsList = string.Concat(locationsList, "[color=#d70aa0]", location, "[/color].");
            else if (nests.Count == 2 && i == 1)
                locationsList = string.Concat(locationsList, "[color=#d70aa0]", location, " ");
            else if (i != nests.Count)
                locationsList = string.Concat(locationsList, "[color=#d70aa0]", location, "[/color], ");
            else
                locationsList = string.Concat(locationsList, $"[/color]and [color=#d70aa0]{location}[/color].");

            totalPoints += pointsStorage.TotalPoints / 10; // dividing by ten gives us a slightly more manageable number + keeps it consistent with pre-stackcount point calculation.

            totalSpawned += pointsStorage.TotalReplicators;

            levels.Add(pointsStorage.Level);
        }

        var highestLevel = levels.Max();

        // then push that summary.
        args.AddLine(Loc.GetString("replicator-nest-end-of-round", ("location", locationsList), ("level", highestLevel), ("points", totalPoints), ("replicators", totalSpawned)));
        args.AddLine("");
    }
}
