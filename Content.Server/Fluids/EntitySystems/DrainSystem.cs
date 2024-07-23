using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.DoAfter;
using Content.Server.Fluids.Components;
using Content.Server.Popups;
using Content.Shared.Audio;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids;
using Content.Shared.Fluids.Components;
using Content.Shared.Interaction;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Collections;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Fluids.EntitySystems;

public sealed class DrainSystem : SharedDrainSystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSoundSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly PuddleSystem _puddleSystem = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DrainComponent, GetVerbsEvent<Verb>>(AddEmptyVerb);
        SubscribeLocalEvent<DrainComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<DrainComponent, AfterInteractUsingEvent>(OnInteract);
        SubscribeLocalEvent<DrainComponent, DrainDoAfterEvent>(OnDoAfter);
    }

    private void AddEmptyVerb(Entity<DrainComponent> entity, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Using == null)
            return;

        if (!TryComp(args.Using, out SpillableComponent? spillable) ||
            !TryComp(args.Target, out DrainComponent? drain))
            return;

        var used = args.Using.Value;
        var target = args.Target;
        Verb verb = new()
        {
            Text = Loc.GetString("drain-component-empty-verb-inhand", ("object", Name(used))),
            Act = () =>
            {
                Empty(used, spillable, target, drain);
            },
            Impact = LogImpact.Low,
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/eject.svg.192dpi.png"))

        };
        args.Verbs.Add(verb);
    }

    private void Empty(EntityUid container, SpillableComponent spillable, EntityUid target, DrainComponent drain)
    {
        // Find the solution in the container that is emptied
        if (!_solutionContainerSystem.TryGetDrainableSolution(container, out var containerSoln, out var containerSolution) || containerSolution.Volume == FixedPoint2.Zero)
        {
            _popupSystem.PopupEntity(
                Loc.GetString("drain-component-empty-verb-using-is-empty-message", ("object", container)),
                container);
            return;
        }

        // try to find the drain's solution
        if (!_solutionContainerSystem.ResolveSolution(target, DrainComponent.SolutionName, ref drain.Solution, out var drainSolution))
        {
            return;
        }

        // Try to transfer as much solution as possible to the drain

        var amountToPutInDrain = drainSolution.AvailableVolume;
        var amountToSpillOnGround = containerSolution.Volume - drainSolution.AvailableVolume;

        if (amountToPutInDrain > 0)
        {
            var solutionToPutInDrain = _solutionContainerSystem.SplitSolution(containerSoln.Value, amountToPutInDrain);
            _solutionContainerSystem.TryAddSolution(drain.Solution.Value, solutionToPutInDrain);

            _audioSystem.PlayPvs(drain.ManualDrainSound, target);
            _ambientSoundSystem.SetAmbience(target, true);
        }


        // Spill the remainder.

        if (amountToSpillOnGround > 0)
        {
            var solutionToSpill = _solutionContainerSystem.SplitSolution(containerSoln.Value, amountToSpillOnGround);
            _puddleSystem.TrySpillAt(Transform(target).Coordinates, solutionToSpill, out _);
            _popupSystem.PopupEntity(
                Loc.GetString("drain-component-empty-verb-target-is-full-message", ("object", target)),
                container);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var managerQuery = GetEntityQuery<SolutionContainerManagerComponent>();
        var xformQuery = GetEntityQuery<TransformComponent>();
        var puddleQuery = GetEntityQuery<PuddleComponent>();
        var puddles = new ValueList<(Entity<PuddleComponent> Entity, string Solution)>();

        var query = EntityQueryEnumerator<DrainComponent>();
        while (query.MoveNext(out var uid, out var drain))
        {
            drain.Accumulator += frameTime;
            if (drain.Accumulator < drain.DrainFrequency)
            {
                continue;
            }
            drain.Accumulator -= drain.DrainFrequency;

            // Disable ambient sound from emptying manually
            if (!drain.AutoDrain)
            {
                _ambientSoundSystem.SetAmbience(uid, false);
                continue;
            }

            if (!managerQuery.TryGetComponent(uid, out var manager))
                continue;

            // Best to do this one every second rather than once every tick...
            if (!_solutionContainerSystem.ResolveSolution((uid, manager), DrainComponent.SolutionName, ref drain.Solution, out var drainSolution))
                continue;

            if (drainSolution.AvailableVolume <= 0)
            {
                _ambientSoundSystem.SetAmbience(uid, false);
                continue;
            }

            // Remove a bit from the buffer
            _solutionContainerSystem.SplitSolution(drain.Solution.Value, (drain.UnitsDestroyedPerSecond * drain.DrainFrequency));

            // This will ensure that UnitsPerSecond is per second...
            var amount = drain.UnitsPerSecond * drain.DrainFrequency;

            if (!xformQuery.TryGetComponent(uid, out var xform))
                continue;

            puddles.Clear();

            foreach (var entity in _lookup.GetEntitiesInRange(_transform.GetMapCoordinates(uid, xform), drain.Range))
            {
                // No InRangeUnobstructed because there's no collision group that fits right now
                // and these are placed by mappers and not buildable/movable so shouldnt really be a problem...
                if (puddleQuery.TryGetComponent(entity, out var puddle))
                {
                    puddles.Add(((entity, puddle), puddle.SolutionName));
                }
            }

            if (puddles.Count == 0)
            {
                _ambientSoundSystem.SetAmbience(uid, false);
                continue;
            }

            _ambientSoundSystem.SetAmbience(uid, true);

            amount /= puddles.Count;

            foreach (var (puddle, solution) in puddles)
            {
                // Queue the solution deletion if it's empty. EvaporationSystem might also do this
                // but queuedelete should be pretty safe.
                if (!_solutionContainerSystem.ResolveSolution(puddle.Owner, solution, ref puddle.Comp.Solution, out var puddleSolution))
                {
                    EntityManager.QueueDeleteEntity(puddle);
                    continue;
                }

                // Removes the lowest of:
                // the drain component's units per second adjusted for # of puddles
                // the puddle's remaining volume (making it cleanly zero)
                // the drain's remaining volume in its buffer.
                var transferSolution = _solutionContainerSystem.SplitSolution(puddle.Comp.Solution.Value,
                    FixedPoint2.Min(FixedPoint2.New(amount), puddleSolution.Volume, drainSolution.AvailableVolume));

                drainSolution.AddSolution(transferSolution, _prototypeManager);

                if (puddleSolution.Volume <= 0)
                {
                    QueueDel(puddle);
                }
            }

            _solutionContainerSystem.UpdateChemicals(drain.Solution.Value);
        }
    }

    private void OnExamined(Entity<DrainComponent> entity, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange ||
            !HasComp<SolutionContainerManagerComponent>(entity) ||
            !_solutionContainerSystem.ResolveSolution(entity.Owner, DrainComponent.SolutionName, ref entity.Comp.Solution, out var drainSolution))
        {
            return;
        }

        var text = drainSolution.AvailableVolume != 0
            ? Loc.GetString("drain-component-examine-volume", ("volume", drainSolution.AvailableVolume))
            : Loc.GetString("drain-component-examine-hint-full");
        args.PushMarkup(text);
    }

    private void OnInteract(Entity<DrainComponent> entity, ref AfterInteractUsingEvent args)
    {
        if (!args.CanReach || args.Target == null ||
            !_tagSystem.HasTag(args.Used, DrainComponent.PlungerTag) ||
            !_solutionContainerSystem.ResolveSolution(args.Target.Value, DrainComponent.SolutionName, ref entity.Comp.Solution, out var drainSolution))
        {
            return;
        }

        if (drainSolution.AvailableVolume > 0)
        {
            _popupSystem.PopupEntity(Loc.GetString("drain-component-unclog-notapplicable", ("object", args.Target.Value)), args.Target.Value);
            return;
        }

        _audioSystem.PlayPvs(entity.Comp.PlungerSound, entity);


        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, entity.Comp.UnclogDuration, new DrainDoAfterEvent(), entity, args.Target, args.Used)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            BreakOnHandChange = true
        };

        _doAfterSystem.TryStartDoAfter(doAfterArgs);
    }

    private void OnDoAfter(Entity<DrainComponent> entity, ref DrainDoAfterEvent args)
    {
        if (args.Target == null)
            return;

        if (!_random.Prob(entity.Comp.UnclogProbability))
        {
            _popupSystem.PopupEntity(Loc.GetString("drain-component-unclog-fail", ("object", args.Target.Value)), args.Target.Value);
            return;
        }


        if (!_solutionContainerSystem.ResolveSolution(args.Target.Value, DrainComponent.SolutionName, ref entity.Comp.Solution))
        {
            return;
        }


        _solutionContainerSystem.RemoveAllSolution(entity.Comp.Solution.Value);
        _audioSystem.PlayPvs(entity.Comp.UnclogSound, args.Target.Value);
        _popupSystem.PopupEntity(Loc.GetString("drain-component-unclog-success", ("object", args.Target.Value)), args.Target.Value);
    }
}
