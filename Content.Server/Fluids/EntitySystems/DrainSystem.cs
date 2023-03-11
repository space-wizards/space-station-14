using System.Linq;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Fluids.Components;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Popups;
using Content.Shared.FixedPoint;
using Content.Shared.Audio;
using Content.Shared.Chemistry.Components;
using Content.Shared.Coordinates;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Collections;

namespace Content.Server.Fluids.EntitySystems
{
    public sealed class DrainSystem : EntitySystem
    {
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionSystem = default!;
        [Dependency] private readonly SharedAmbientSoundSystem _ambientSoundSystem = default!;
        [Dependency] private readonly AudioSystem _audioSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly SpillableSystem _spillableSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DrainComponent, GetVerbsEvent<Verb>>(AddEmptyVerb);
            SubscribeLocalEvent<DrainComponent, ExaminedEvent>(OnExamined);
        }

        private void AddEmptyVerb(EntityUid uid, DrainComponent component, GetVerbsEvent<Verb> args)
        {
            if (!args.CanAccess || !args.CanInteract || args.Using == null)
                return;

            if (!TryComp(args.Using, out SpillableComponent? spillable) ||
                !TryComp(args.Target, out DrainComponent? drain))
                return;

            Verb verb = new()
            {
                Text = Loc.GetString("drain-component-empty-verb-inhand", ("object", "" + Name(args.Using.Value))),
                Act = () =>
                {
                    Empty(args.Using.Value, spillable, args.Target, drain);
                },
                Impact = LogImpact.Low,

            };
            args.Verbs.Add(verb);
        }

        private void Empty(EntityUid container, SpillableComponent spillable, EntityUid target, DrainComponent drain)
        {
            // Find the solution in the container that is emptied
            if (!_solutionSystem.TryGetDrainableSolution(container, out var containerSolution) ||
                containerSolution.Volume == FixedPoint2.Zero)
            {
                _popupSystem.PopupEntity(
                    Loc.GetString("drain-component-empty-verb-using-is-empty-message", ("object", container)),
                    container);
                return;
            }

            // try to find the drain's solution
            if (!_solutionSystem.TryGetSolution(target, DrainComponent.SolutionName, out var drainSolution))
            {
                // If can't find, just destroy the solution
                containerSolution.RemoveAllSolution();
                _audioSystem.PlayPvs(drain.ManualDrainSound, target);
                _ambientSoundSystem.SetAmbience(target, true);
                return;
            }

            // Try to transfer as much solution as possible to the drain

            var transferSolution = _solutionSystem.SplitSolution(container, containerSolution,
                FixedPoint2.Min(containerSolution.Volume, drainSolution.AvailableVolume));

            _solutionSystem.TryAddSolution(target, drainSolution, transferSolution);
            _solutionSystem.UpdateAppearance(target, drainSolution);
            _solutionSystem.UpdateAppearance(container, containerSolution);

            _audioSystem.PlayPvs(drain.ManualDrainSound, target);
            _ambientSoundSystem.SetAmbience(target, true);

            // If drain is full, spill

            if (drainSolution.MaxVolume == drainSolution.Volume)
            {
                _spillableSystem.SpillAt(containerSolution, Transform(target).Coordinates, "PuddleSmear");
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
            var puddles = new ValueList<(EntityUid Entity, string Solution)>();

            foreach (var drain in EntityQuery<DrainComponent>())
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
                    _ambientSoundSystem.SetAmbience(drain.Owner, false);
                    return;
                }

                if (!managerQuery.TryGetComponent(drain.Owner, out var manager))
                    continue;

                // Best to do this one every second rather than once every tick...
                _solutionSystem.TryGetSolution(drain.Owner, DrainComponent.SolutionName, out var drainSolution, manager);

                if (drainSolution is null)
                    continue;

                if (drainSolution.AvailableVolume <= 0)
                {
                    _ambientSoundSystem.SetAmbience(drain.Owner, false);
                    continue;
                }

                // Remove a bit from the buffer
                _solutionSystem.SplitSolution(drain.Owner, drainSolution, (drain.UnitsDestroyedPerSecond * drain.DrainFrequency));

                // This will ensure that UnitsPerSecond is per second...
                var amount = drain.UnitsPerSecond * drain.DrainFrequency;

                if (!xformQuery.TryGetComponent(drain.Owner, out var xform))
                    continue;

                puddles.Clear();

                foreach (var entity in _lookup.GetEntitiesInRange(xform.MapPosition, drain.Range))
                {
                    // No InRangeUnobstructed because there's no collision group that fits right now
                    // and these are placed by mappers and not buildable/movable so shouldnt really be a problem...
                    if (puddleQuery.TryGetComponent(entity, out var puddle))
                    {
                        puddles.Add((entity, puddle.SolutionName));
                    }
                }

                if (puddles.Count == 0)
                {
                    _ambientSoundSystem.SetAmbience(drain.Owner, false);
                    continue;
                }

                _ambientSoundSystem.SetAmbience(drain.Owner, true);

                amount /= puddles.Count;

                foreach (var (puddle, solution) in puddles)
                {
                    // Queue the solution deletion if it's empty. EvaporationSystem might also do this
                    // but queuedelete should be pretty safe.
                    if (!_solutionSystem.TryGetSolution(puddle, solution, out var puddleSolution))
                    {
                        EntityManager.QueueDeleteEntity(puddle);
                        continue;
                    }

                    // Removes the lowest of:
                    // the drain component's units per second adjusted for # of puddles
                    // the puddle's remaining volume (making it cleanly zero)
                    // the drain's remaining volume in its buffer.
                    var transferSolution = _solutionSystem.SplitSolution(puddle, puddleSolution,
                        FixedPoint2.Min(FixedPoint2.New(amount), puddleSolution.Volume, drainSolution.AvailableVolume));

                    _solutionSystem.TryAddSolution(drain.Owner, drainSolution, transferSolution);

                    if (puddleSolution.Volume <= 0)
                    {
                        QueueDel(puddle);
                    }
                }
            }
        }

        private void OnExamined(EntityUid uid, DrainComponent component, ExaminedEvent args)
        {
            if (!args.IsInDetailsRange) { return; }
            if (!TryComp(uid, out SolutionContainerManagerComponent? solutionComp)) { return; }
            if (!_solutionSystem.TryGetSolution(uid, DrainComponent.SolutionName, out var drainSolution)) { return; }

            var text = drainSolution.AvailableVolume != 0 ?
                Loc.GetString("drain-component-examine-volume", ("volume", drainSolution.AvailableVolume)) :
                Loc.GetString("drain-component-examine-hint-full");
            args.Message.AddMarkup($"\n\n{text}");
        }
    }
}
