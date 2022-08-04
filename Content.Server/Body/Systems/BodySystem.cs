using Content.Server.Body.Components;
using Content.Server.GameTicking;
using Content.Server.Kitchen.Components;
using Content.Server.Mind.Components;
using Content.Server.MobState;
using Content.Shared.Audio;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Prototypes;
using Content.Shared.Body.Systems.Body;
using Content.Shared.Movement.Events;
using Content.Shared.Random.Helpers;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Body.Systems
{
    public sealed class BodySystem : SharedBodySystem
    {
        [Dependency] private readonly GameTicker _ticker = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly BodyPartSystem _bodyPartSystem = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
        [Dependency] private readonly AudioSystem _audioSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<BodyComponent, MoveInputEvent>(OnMoveInput);

            SubscribeLocalEvent<BodyComponent, ApplyMetabolicMultiplierEvent>(OnApplyMetabolicMultiplier);
            SubscribeLocalEvent<BodyComponent, BeingMicrowavedEvent>(OnBeingMicrowaved);
        }

        #region Body events

        protected override void OnComponentInit(EntityUid uid, SharedBodyComponent component, ComponentInit args)
        {
            base.OnComponentInit(uid, component, args);

            if (string.IsNullOrEmpty(component.PresetId) ||
                !PrototypeManager.TryIndex<BodyPresetPrototype>(component.PresetId, out var preset))
                return;

            foreach (var slot in component.Slots.Values)
            {
                if (slot.HasPart)
                    continue;

                if (!preset.PartIDs.TryGetValue(slot.Id, out var partId))
                    continue;

                var part = Spawn(partId, Transform(component.Owner).Coordinates);
                AddPartAndRaiseEvents(part, slot, component);
            }
        }

        private void OnMoveInput(EntityUid uid, BodyComponent component, ref MoveInputEvent args)
        {
            if (_mobStateSystem.IsDead(uid) &&
                TryComp<MindComponent>(uid, out var mind) &&
                mind.HasMind)
            {
                if (!mind.Mind!.TimeOfDeath.HasValue)
                {
                    mind.Mind.TimeOfDeath = _gameTiming.RealTime;
                }

                _ticker.OnGhostAttempt(mind.Mind!, true);
            }
        }

        private void OnApplyMetabolicMultiplier(EntityUid uid, BodyComponent component, ApplyMetabolicMultiplierEvent args)
        {
            foreach (var part in GetAllParts(uid, component))
                foreach (var mechanism in _bodyPartSystem.GetAllMechanisms(part.Owner, part))
                {
                    RaiseLocalEvent(mechanism.Owner, args, false);
                }
        }

        private void OnBeingMicrowaved(EntityUid uid, BodyComponent component, BeingMicrowavedEvent args)
        {
            if (args.Handled)
                return;

            // Don't microwave animals, kids
            Transform(uid).AttachToGridOrMap();
            Gib(uid, true, component);

            args.Handled = true;
        }

        #endregion

        public HashSet<EntityUid> Gib(EntityUid uid, bool gibParts = false,
        BodyComponent? body = null)
        {
            var gibs = new HashSet<EntityUid>();
            if (!Resolve(uid, ref body, false))
                return gibs;

            foreach (var (slot, part) in GetAllSlotsWithPart(uid, body))
            {
                gibs.Add(part.Owner);
                RemovePart(uid, slot, body);

                if (gibParts)
                    gibs.UnionWith(_bodyPartSystem.Gib(part.Owner, part));
            }

            foreach (var part in gibs)
            {
                RaiseLocalEvent(part, new PartGibbedEvent(uid, gibs), true);
            }

            _audioSystem.Play(body.GibSound, Filter.Pvs(uid), Transform(uid).Coordinates, AudioHelpers.WithVariation(0.025f, _random));

            if (TryComp(uid, out ContainerManagerComponent? container))
            {
                foreach (var cont in container.GetAllContainers())
                {
                    foreach (var ent in cont.ContainedEntities)
                    {
                        cont.ForceRemove(ent);
                        Transform(ent).Coordinates = Transform(uid).Coordinates;
                        ent.RandomOffset(0.25f);
                    }
                }
            }

            RaiseLocalEvent(uid, new BeingGibbedEvent(gibs));
            QueueDel(uid);

            return gibs;
        }
    }
}
