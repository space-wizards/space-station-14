using Content.Shared.Audio;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Fluids.EntitySystems;

/// <summary>
/// Handles the draining of solutions from containers into drains.
/// </summary>
public sealed class DrainSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSound = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPuddleSystem _puddle = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly HashSet<Entity<PuddleComponent>> _puddles = [];

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DrainComponent, MapInitEvent>(OnDrainMapInit);
        SubscribeLocalEvent<DrainComponent, GetVerbsEvent<Verb>>(AddEmptyVerb);
        SubscribeLocalEvent<DrainComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<DrainComponent, AfterInteractUsingEvent>(OnInteract);
        SubscribeLocalEvent<DrainComponent, DrainDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<DrainComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
    }

    private void OnDrainMapInit(Entity<DrainComponent> ent, ref MapInitEvent args)
    {
        // Randomise puddle drains so roundstart ones don't all dump at the same time.
        ent.Comp.NextUpdate = _timing.CurTime + _random.Next(ent.Comp.DrainInterval);
        Dirty(ent);
    }

    private void AddEmptyVerb(Entity<DrainComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Using == null)
            return;

        if (!HasComp<SpillableComponent>(args.Using) ||
            !TryComp<DrainComponent>(args.Target, out var drain))
            return;

        var user = args.User;
        var used = args.Using.Value;
        var target = args.Target;
        Verb verb = new()
        {
            Text = Loc.GetString("drain-component-empty-verb-inhand", ("object", Name(used))),
            Act = () =>
            {
                Empty((target, drain), user, used);
            },
            Impact = LogImpact.Low,
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/eject.svg.192dpi.png"))
        };
        args.Verbs.Add(verb);
    }

    private void Empty(Entity<DrainComponent> ent, EntityUid user, EntityUid container)
    {
        // Find the solution in the container that is emptied.
        if (!_solutionContainerSystem.TryGetDrainableSolution(container, out var containerSoln, out var containerSolution) || containerSolution.Volume == FixedPoint2.Zero)
        {
            _popup.PopupClient(
                Loc.GetString("drain-component-empty-verb-using-is-empty-message", ("object", container)),
                ent.Owner,
                user);
            return;
        }

        // Try to find the drain's solution.
        if (!_solutionContainerSystem.ResolveSolution(ent.Owner, DrainComponent.SolutionName, ref ent.Comp.Solution, out var drainSolution))
            return;

        // Try to transfer as much solution as possible to the drain.
        var amountToPutInDrain = drainSolution.AvailableVolume;
        var amountToSpillOnGround = containerSolution.Volume - drainSolution.AvailableVolume;

        if (amountToPutInDrain > 0)
        {
            var solutionToPutInDrain = _solutionContainerSystem.SplitSolution(containerSoln.Value, amountToPutInDrain);
            _solutionContainerSystem.TryAddSolution(ent.Comp.Solution.Value, solutionToPutInDrain);

            _audio.PlayPredicted(ent.Comp.ManualDrainSound, ent.Owner, user);
            _ambientSound.SetAmbience(ent.Owner, true);
        }

        // Spill the remainder.
        if (amountToSpillOnGround > 0)
        {
            var solutionToSpill = _solutionContainerSystem.SplitSolution(containerSoln.Value, amountToSpillOnGround);
            _puddle.TrySpillAt(Transform(ent.Owner).Coordinates, solutionToSpill, out _);
            _popup.PopupClient(
                Loc.GetString("drain-component-empty-verb-target-is-full-message", ("object", ent.Owner)),
                ent.Owner,
                user);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<DrainComponent, SolutionContainerManagerComponent>();
        var curTime = _timing.CurTime;
        while (query.MoveNext(out var uid, out var drain, out var manager))
        {
            if (curTime < drain.NextUpdate)
                continue;

            drain.NextUpdate += drain.DrainInterval;
            Dirty(uid, drain);

            // Best to do this one every second rather than once every tick...
            if (!_solutionContainerSystem.ResolveSolution((uid, manager), DrainComponent.SolutionName, ref drain.Solution, out var drainSolution))
                continue;

            if (drainSolution.Volume <= 0 && !drain.AutoDrain)
            {
                _ambientSound.SetAmbience(uid, false);
                continue;
            }

            // Remove a bit from the buffer.
            _solutionContainerSystem.SplitSolution(drain.Solution.Value, drain.UnitsDestroyedPerSecond * drain.DrainInterval.TotalSeconds);

            // This will ensure that UnitsPerSecond is per second...
            var amount = drain.UnitsPerSecond * drain.DrainInterval.TotalSeconds;

            if (drain.AutoDrain)
            {
                _puddles.Clear();
                _lookup.GetEntitiesInRange(Transform(uid).Coordinates, drain.Range, _puddles);

                if (_puddles.Count == 0 && drainSolution.Volume <= 0)
                {
                    _ambientSound.SetAmbience(uid, false);
                    continue;
                }

                _ambientSound.SetAmbience(uid, true);

                amount /= _puddles.Count;

                foreach (var puddle in _puddles)
                {
                    // Queue the solution deletion if it's empty. EvaporationSystem might also do this
                    // but queuedelete should be pretty safe.
                    if (!_solutionContainerSystem.ResolveSolution(puddle.Owner, puddle.Comp.SolutionName, ref puddle.Comp.Solution, out var puddleSolution))
                    {
                        PredictedQueueDel(puddle);
                        continue;
                    }

                    // Removes the lowest of:
                    // the drain component's units per second adjusted for # of puddles
                    // the puddle's remaining volume (making it cleanly zero)
                    // the drain's remaining volume in its buffer.
                    var transferSolution = _solutionContainerSystem.SplitSolution(puddle.Comp.Solution.Value,
                        FixedPoint2.Min(FixedPoint2.New(amount), puddleSolution.Volume, drainSolution.AvailableVolume));

                    drainSolution.AddSolution(transferSolution, _prototype);

                    if (puddleSolution.Volume <= 0)
                        PredictedQueueDel(puddle);
                }
            }

            _solutionContainerSystem.UpdateChemicals(drain.Solution.Value);
        }
    }

    private void OnExamined(Entity<DrainComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange ||
            !HasComp<SolutionContainerManagerComponent>(ent) ||
            !_solutionContainerSystem.ResolveSolution(ent.Owner, DrainComponent.SolutionName, ref ent.Comp.Solution, out var drainSolution))
        {
            return;
        }

        var text = drainSolution.AvailableVolume != 0
            ? Loc.GetString("drain-component-examine-volume", ("volume", drainSolution.AvailableVolume))
            : Loc.GetString("drain-component-examine-hint-full");
        args.PushMarkup(text);
    }

    private void OnInteract(Entity<DrainComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (!args.CanReach || args.Target == null ||
            !_tag.HasTag(args.Used, DrainComponent.PlungerTag) ||
            !_solutionContainerSystem.ResolveSolution(args.Target.Value, DrainComponent.SolutionName, ref ent.Comp.Solution, out var drainSolution))
        {
            return;
        }

        if (drainSolution.AvailableVolume > 0)
        {
            _popup.PopupPredicted(Loc.GetString("drain-component-unclog-notapplicable", ("object", args.Target.Value)), args.Target.Value, args.User);
            return;
        }

        _audio.PlayPredicted(ent.Comp.PlungerSound, ent.Owner, args.User);

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, ent.Comp.UnclogDuration, new DrainDoAfterEvent(), ent, args.Target, args.Used)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            BreakOnHandChange = true,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnDoAfter(Entity<DrainComponent> ent, ref DrainDoAfterEvent args)
    {
        if (args.Target == null)
            return;

        // TODO: Replace with RandomPredicted once the engine PR is merged
        var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, ent.Owner.GetHashCode());
        var rand = new System.Random(seed);
        if (!rand.Prob(ent.Comp.UnclogProbability))
        {
            _popup.PopupPredicted(Loc.GetString("drain-component-unclog-fail", ("object", args.Target.Value)), args.Target.Value, args.User);
            return;
        }

        if (!_solutionContainerSystem.ResolveSolution(args.Target.Value, DrainComponent.SolutionName, ref ent.Comp.Solution))
            return;

        _solutionContainerSystem.RemoveAllSolution(ent.Comp.Solution.Value);
        _audio.PlayPredicted(ent.Comp.UnclogSound, args.Target.Value, args.User);
        _popup.PopupPredicted(Loc.GetString("drain-component-unclog-success", ("object", args.Target.Value)), args.Target.Value, args.User);
    }

    // Prevent a debug assert.
    // See https://github.com/space-wizards/space-station-14/pull/35314
    private void OnEntRemoved(Entity<DrainComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        // Make sure the removed entity was our contained solution
        if (ent.Comp.Solution is not { } solution || args.Entity != solution.Owner)
            return;

        // Cleared our cached reference to the solution entity
        ent.Comp.Solution = null;
    }
}

/// <summary>
/// Event raised when a do-after action for unclogging a drain completes.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class DrainDoAfterEvent : SimpleDoAfterEvent;
