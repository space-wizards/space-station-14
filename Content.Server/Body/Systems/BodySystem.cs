using System.Linq;
using Content.Server.Body.Components;
using Content.Server.GameTicking;
using Content.Server.Kitchen.Components;
using Content.Server.Mind.Components;
using Content.Shared.Audio;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Part;
using Content.Shared.Body.Prototypes;
using Content.Shared.Body.Systems.Body;
using Content.Shared.MobState.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Random.Helpers;
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

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<BodyComponent, MoveInputEvent>(OnMoveInput);
            SubscribeLocalEvent<BodyComponent, ComponentStartup>(OnComponentStartup);

            SubscribeLocalEvent<FallDownNoLegsComponent, ComponentInit>(OnFallDownInit);
            SubscribeLocalEvent<FallDownNoLegsComponent, PartRemovedFromBodyEvent>(OnFallDownPartRemoved);
            SubscribeLocalEvent<BodyComponent, ApplyMetabolicMultiplierEvent>(OnApplyMetabolicMultiplier);
            SubscribeLocalEvent<BodyComponent, BeingMicrowavedEvent>(OnBeingMicrowaved);
        }

        #region Overrides

        protected override void OnComponentInit(EntityUid uid, SharedBodyComponent component, ComponentInit args)
        {
            base.OnComponentInit(uid, component, args);

            var preset = _prototypeManager.Index<BodyPresetPrototype>(component.PresetId);
            foreach (var slot in component.SlotIds.Values)
            {
                // Using MapPosition instead of Coordinates here prevents
                // a crash within the character preview menu in the lobby
                var entity = Spawn(preset.PartIDs[slot.Id], Transform(uid).MapPosition);

                if (!TryComp(entity, out SharedBodyPartComponent? part))
                {
                    Logger.Error($"Entity {slot.Id} does not have a {nameof(SharedBodyPartComponent)} component.");
                    continue;
                }

                AddPart(uid, slot.Id, part, component);
            }
        }

        protected override void OnPartAdded(SharedBodyComponent body, SharedBodyPartComponent part)
        {
            body.PartContainer.Insert(part.Owner);
        }

        protected override void OnPartRemoved(SharedBodyComponent body, SharedBodyPartComponent part)
        {
            body.PartContainer.Remove(part.Owner);
            part.Owner.RandomOffset(0.25f);
        }

        #endregion

        #region Body events

        private void OnComponentStartup(EntityUid uid, BodyComponent component, ComponentStartup args)
        {
            // This is ran in Startup as entities spawned in Initialize
            // are not synced to the client since they are assumed to be
            // identical on it

            // TODO that should probably get sussed out given we're removing startup eventually.
            foreach (var (part, _) in component.Parts)
            {
                part.Dirty();
            }
        }

        private void OnMoveInput(EntityUid uid, BodyComponent component, ref MoveInputEvent args)
        {
            if (EntityManager.TryGetComponent<MobStateComponent>(uid, out var mobState) &&
                mobState.IsDead() &&
                EntityManager.TryGetComponent<MindComponent>(uid, out var mind) &&
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
            foreach (var (part, _) in component.Parts)
                foreach (var mechanism in part.Mechanisms)
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

            if (!TryComp<SharedBodyPartComponent>(args.BodyPartUid, out var part))
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

            foreach (var part in body.Parts.Keys.ToList())
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
