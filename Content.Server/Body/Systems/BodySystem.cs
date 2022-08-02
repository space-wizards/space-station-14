using System.Linq;
using Content.Server.Body.Components;
using Content.Server.GameTicking;
using Content.Server.Kitchen.Components;
using Content.Server.Mind.Components;
using Content.Server.MobState;
using Content.Shared.Audio;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Part;
using Content.Shared.Body.Prototypes;
using Content.Shared.Body.Systems.Body;
using Content.Shared.MobState.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Random.Helpers;
using Content.Shared.Standing;
using Robust.Shared.Audio;
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
        [Dependency] private readonly StandingStateSystem _standingStateSystem = default!;
        [Dependency] private readonly MobStateSystem _mobStateSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<BodyComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<BodyComponent, MoveInputEvent>(OnMoveInput);

            SubscribeLocalEvent<FallDownNoLegsComponent, ComponentInit>(OnFallDownInit);
            SubscribeLocalEvent<FallDownNoLegsComponent, PartRemovedFromBodyEvent>(OnFallDownPartRemoved);
            SubscribeLocalEvent<BodyComponent, ApplyMetabolicMultiplierEvent>(OnApplyMetabolicMultiplier);
            SubscribeLocalEvent<BodyComponent, BeingMicrowavedEvent>(OnBeingMicrowaved);
        }

        #region Body events

        private void OnMapInit(EntityUid uid, BodyComponent body, MapInitEvent args)
        {
            if (string.IsNullOrEmpty(body.PresetId) ||
                !PrototypeManager.TryIndex<BodyPresetPrototype>(body.PresetId, out var preset))
                return;

            foreach (var slot in body.Slots.Values)
            {
                if (slot.HasPart)
                    continue;

                if (!preset.PartIDs.TryGetValue(slot.Id, out var partId))
                    continue;

                var part = Spawn(partId, Transform(body.Owner).Coordinates);
                slot.ContainerSlot?.Insert(part);
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

        #region Fall down events

        private void OnFallDownPartRemoved(EntityUid uid, FallDownNoLegsComponent component, PartRemovedFromBodyEvent args)
        {
            if (!TryComp<SharedBodyComponent>(uid, out var body))
                return;

            if (!TryComp<SharedBodyPartComponent>(args.BodyPart, out var part))
                return;

            if (part.PartType == BodyPartType.Leg &&
                GetPartsOfType(uid, BodyPartType.Leg, body).ToArray().Length == 0)
            {
                _standingStateSystem.Down(uid);
            }
        }

        private void OnFallDownInit(EntityUid uid, FallDownNoLegsComponent component, ComponentInit args)
        {
            if (!TryComp<SharedBodyComponent>(uid, out var body))
                return;

            // if you spawn with no legs, then..
            if (GetPartsOfType(uid, BodyPartType.Leg, body).ToArray().Length == 0)
            {
                _standingStateSystem.Down(uid);
            }
        }

        #endregion

        public HashSet<EntityUid> Gib(EntityUid uid, bool gibParts = false,
        BodyComponent? body = null)
        {
            var gibs = new HashSet<EntityUid>();
            if (!Resolve(uid, ref body, false))
                return gibs;

            foreach (var part in GetAllParts(uid, body))
            {
                gibs.Add(part.Owner);
                RemovePart(uid, part, body);

                if (gibParts)
                    gibs.UnionWith(_bodyPartSystem.Gib(uid, part));
            }

            foreach (var part in gibs)
            {
                RaiseLocalEvent(part, new PartGibbedEvent(uid, gibs), true);
            }

            SoundSystem.Play(body.GibSound.GetSound(), Filter.Pvs(uid), Transform(uid).Coordinates, AudioHelpers.WithVariation(0.025f, _random));

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
