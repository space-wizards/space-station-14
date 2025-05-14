// these are HEAVILY based on the Bingle free-agent ghostrole from GoobStation, but reflavored and reprogrammed to make them more Robust (and less of a meme.)
// all credit for the core gameplay concepts and a lot of the core functionality of the code goes to the folks over at Goob, but I re-wrote enough of it to justify putting it in our filestructure.
// the original Bingle PR can be found here: https://github.com/Goob-Station/Goob-Station/pull/1519

using Content.Server.Actions;
using Content.Server.Ghost.Roles.Events;
using Content.Server.Popups;
using Content.Shared._Impstation.Replicator;
using Content.Shared._Impstation.SpawnedFromTracker;
using Content.Shared.Actions;
using Content.Shared.CombatMode;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server._Impstation.Replicator;

public sealed class ReplicatorSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReplicatorComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<ReplicatorComponent, AttackAttemptEvent>(OnAttackAttempt);
        SubscribeLocalEvent<ReplicatorComponent, ToggleCombatActionEvent>(OnCombatToggle);
        SubscribeLocalEvent<ReplicatorComponent, GhostRoleSpawnerUsedEvent>(OnGhostRoleSpawnerUsed);
        SubscribeLocalEvent<ReplicatorComponent, ReplicatorSpawnNestActionEvent>(OnSpawnNestAction);
    }

    private void OnMindAdded(Entity<ReplicatorComponent> ent, ref MindAddedMessage args)
    {
        if (ent.Comp.HasSpawnedNest)
            return;

        if (ent.Comp.Queen) // if you're the queen, which you'll only be if you're the first one spawned,
        {
            // give the action to spawn a nest.
            if (!TryComp<MindContainerComponent>(ent, out var mindContainer) || mindContainer.Mind == null)
                return;

            if (!mindContainer.HasMind)
                _actions.AddAction((EntityUid)ent, ent.Comp.SpawnNewNestAction);
            else
                _actionContainer.AddAction((EntityUid)mindContainer.Mind, ent.Comp.SpawnNewNestAction);

            ent.Comp.HasSpawnedNest = true;
        }
    }

    private void OnSpawnNestAction(Entity<ReplicatorComponent> ent, ref ReplicatorSpawnNestActionEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var xform = Transform(ent);
        var coords = xform.Coordinates;

        if (!coords.IsValid(EntityManager) || xform.MapID == MapId.Nullspace)
            return;

        // spawn a nest, then make sure it has ReplicatorNestComponent
        var myNest = Spawn("ReplicatorNest", xform.Coordinates);
        var myNestComp = EnsureComp<ReplicatorNestComponent>(myNest);

        // then set that nest's spawned minions to our saved list of related replicators.
        HashSet<EntityUid> newMinions = [];
        foreach (var (uid, _) in ent.Comp.RelatedReplicators)
        {
            newMinions.Add(uid);
        }
        myNestComp.SpawnedMinions = newMinions;
        // make sure the nest knows who we are, and vice versa.
        myNestComp.SpawnedMinions.Add(ent);
        ent.Comp.MyNest = myNest;
        // and we don't need the RelatedReplicators list anymore, so,
        ent.Comp.RelatedReplicators.Clear();

        // then we need to remove the action, to ensure it can't be used infinitely.
        QueueDel(args.Action);
    }

    private void OnGhostRoleSpawnerUsed(Entity<ReplicatorComponent> ent, ref GhostRoleSpawnerUsedEvent args)
    {
        if (!TryComp<SpawnedFromTrackerComponent>(args.Spawner, out var tracker) || !TryComp<ReplicatorNestComponent>(tracker.SpawnedFrom, out var nestComp))
            return;
        // add the spawned replicator to the nest's list when someone takes the ghostrole.
        nestComp.SpawnedMinions.Add(ent);
        // then remove the spawner from the nest's list of unclaimed spawners.
        nestComp.UnclaimedSpawners.Remove(args.Spawner);

        // finally, tell the new fella who they momma is
        ent.Comp.MyNest = tracker.SpawnedFrom;
    }

    private void OnAttackAttempt(Entity<ReplicatorComponent> ent, ref AttackAttemptEvent args)
    {
        // Can't attack your friends.
        if (HasComp<ReplicatorComponent>(args.Target))
        {
            _popup.PopupEntity(Loc.GetString("replicator-on-replicator-attack-fail"), ent, ent, PopupType.MediumCaution);
            args.Cancel();
        }

        // Can't attack the nest.
        if (HasComp<ReplicatorNestComponent>(args.Target))
        {
            _popup.PopupEntity(Loc.GetString("replicator-on-nest-attack-fail"), ent, ent, PopupType.MediumCaution);
            args.Cancel();
        }
    }

    private void OnCombatToggle(Entity<ReplicatorComponent> ent, ref ToggleCombatActionEvent args)
    {
        if (!TryComp<CombatModeComponent>(ent, out var combat))
            return;

        // visual indicator that the replicator is aggressive. 
        _appearance.SetData(ent, ReplicatorVisuals.Combat, combat.IsInCombatMode);
    }
}
