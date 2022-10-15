using System.Diagnostics.CodeAnalysis;
using Content.Server.Body.Components;
using Content.Server.GameTicking;
using Content.Server.Humanoid;
using Content.Server.Kitchen.Components;
using Content.Server.Mind.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Humanoid;
using Content.Shared.MobState.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Random.Helpers;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.Body.Systems
{
    public sealed class BodySystem : SharedBodySystem
    {
        [Dependency] private readonly GameTicker _ticker = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        [Dependency] private readonly HumanoidSystem _humanoidSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<BodyComponent, MoveInputEvent>(OnRelayMoveInput);
            SubscribeLocalEvent<BodyComponent, ApplyMetabolicMultiplierEvent>(OnApplyMetabolicMultiplier);
            SubscribeLocalEvent<BodyComponent, BeingMicrowavedEvent>(OnBeingMicrowaved);
        }

        private void OnRelayMoveInput(EntityUid uid, BodyComponent component, ref MoveInputEvent args)
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
            foreach (var organ in GetChildOrgans(uid, component))
            {
                RaiseLocalEvent(organ.Owner, args);
            }
        }

        private void OnBeingMicrowaved(EntityUid uid, BodyComponent component, BeingMicrowavedEvent args)
        {
            if (args.Handled)
                return;

            // Don't microwave animals, kids
            Transform(uid).AttachToGridOrMap();
            Gib(uid, false, component);

            args.Handled = true;
        }

        public override bool Attach(EntityUid? partId, BodyPartSlot slot, [NotNullWhen(true)] BodyComponent? part = null)
        {
            if (!base.Attach(partId, slot, part))
                return false;

            if (TryGetRoot(partId, out var root, part) &&
                TryComp<HumanoidComponent>(root.Owner, out var humanoid))
            {
                var layer = root.ToHumanoidLayers();
                if (layer != null)
                {
                    var layers = HumanoidVisualLayersExtension.Sublayers(layer.Value);
                    _humanoidSystem.SetLayersVisibility(root.Owner, layers, true, true, humanoid);
                }
            }

            return true;
        }

        public override bool Drop(EntityUid? partId, BodyComponent? part = null)
        {
            var root = GetRoot(partId, part);

            if (!base.Drop(partId, part))
                return false;

            if (root != null && TryComp<HumanoidComponent>(root.Owner, out var humanoid))
            {
                var layer = root.ToHumanoidLayers();
                if (layer != null)
                {
                    var layers = HumanoidVisualLayersExtension.Sublayers(layer.Value);
                    _humanoidSystem.SetLayersVisibility(root.Owner, layers, false, true, humanoid);
                }
            }

            return true;
        }

        public override HashSet<EntityUid> Gib(EntityUid? partId, bool gibOrgans = false, BodyComponent? part = null)
        {
            var root = GetRoot(partId, part);
            var gibs =  base.Gib(partId, gibOrgans, part);

            if (root != null)
            {
                var xform = Transform(root.Owner);
                var coordinates = xform.Coordinates;

                // These have already been forcefully removed from containers so run it here.
                foreach (var gib in gibs)
                {
                    RaiseLocalEvent(gib, new PartGibbedEvent(root.Owner, gibs), true);
                }

                _audio.Play(root.GibSound, Filter.Pvs(root.Owner, entityManager: EntityManager), coordinates, AudioParams.Default.WithVariation(0.025f));

                if (TryComp(root.Owner, out ContainerManagerComponent? container))
                {
                    foreach (var cont in container.GetAllContainers())
                    {
                        foreach (var ent in cont.ContainedEntities)
                        {
                            cont.ForceRemove(ent);
                            Transform(ent).Coordinates = coordinates;
                            ent.RandomOffset(0.25f);
                        }
                    }
                }

                RaiseLocalEvent(root.Owner, new BeingGibbedEvent(gibs));
                QueueDel(root.Owner);
            }

            return gibs;
        }

        public bool Orphan(EntityUid? partId, BodyComponent? part = null)
        {
            if (partId == null ||
                !Resolve(partId.Value, ref part, false))
                return false;

            Drop(partId);

            foreach (var slot in part.Children.Values)
            {
                Drop(slot.Child);
            }

            return false;
        }
    }
}
