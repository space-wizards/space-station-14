using Content.Server.Fluids.EntitySystems;
using Content.Server.Nutrition.Components;
using Content.Server.Popups;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.IdentityManagement;
using Content.Shared.Nutrition;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Rejuvenate;
using Content.Shared.Throwing;
using Content.Shared.Trigger.Components;
using Content.Shared.Trigger.Systems;
using Content.Shared.Chemistry.EntitySystems;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Server.Nutrition.EntitySystems
{
    [UsedImplicitly]
    public sealed class CreamPieSystem : SharedCreamPieSystem
    {
        [Dependency] private readonly IngestionSystem _ingestion = default!;
        [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly PuddleSystem _puddle = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedSolutionContainerSystem _solutions = default!;
        [Dependency] private readonly TriggerSystem _trigger = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<CreamPieComponent, SliceFoodEvent>(OnSlice);

            SubscribeLocalEvent<CreamPiedComponent, RejuvenateEvent>(OnRejuvenate);
        }

        protected override void SplattedCreamPie(Entity<CreamPieComponent, EdibleComponent?> entity)
        {
            // The entity is deleted, so play the sound at its position rather than parenting
            var coordinates = Transform(entity).Coordinates;
            _audio.PlayPvs(_audio.ResolveSound(entity.Comp1.Sound), coordinates, AudioParams.Default.WithVariation(0.125f));

            if (Resolve(entity, ref entity.Comp2, false))
            {
                if (_solutions.TryGetSolution(entity.Owner, entity.Comp2.Solution, out _, out var solution))
                    _puddle.TrySpillAt(entity.Owner, solution, out _, false);

                _ingestion.SpawnTrash((entity, entity.Comp2));
            }

            ActivatePayload(entity);

            QueueDel(entity);
        }

        // TODO
        // A regression occured here. Previously creampies would activate their hidden payload if you tried to eat them.
        // However, the refactor to IngestionSystem caused the event to not be reached,
        // because eating is blocked if an item is inside the food.

        private void OnSlice(Entity<CreamPieComponent> entity, ref SliceFoodEvent args)
        {
            ActivatePayload(entity);
        }

        private void ActivatePayload(EntityUid uid)
        {
            if (_itemSlots.TryGetSlot(uid, CreamPieComponent.PayloadSlotName, out var itemSlot))
            {
                if (_itemSlots.TryEject(uid, itemSlot, user: null, out var item))
                {
                    if (TryComp<TimerTriggerComponent>(item.Value, out var timerTrigger))
                    {
                        _trigger.ActivateTimerTrigger((item.Value, timerTrigger));
                    }
                }
            }
        }

        protected override void CreamedEntity(EntityUid uid, CreamPiedComponent creamPied, ThrowHitByEvent args)
        {
            _popup.PopupEntity(Loc.GetString("cream-pied-component-on-hit-by-message",
                                            ("thrown", Identity.Entity(args.Thrown, EntityManager))),
                                            uid, args.Target);

            var otherPlayers = Filter.PvsExcept(uid);

            _popup.PopupEntity(Loc.GetString("cream-pied-component-on-hit-by-message-others",
                                            ("owner", Identity.Entity(uid, EntityManager)),
                                            ("thrown", Identity.Entity(args.Thrown, EntityManager))),
                                            uid, otherPlayers, false);
        }

        private void OnRejuvenate(Entity<CreamPiedComponent> entity, ref RejuvenateEvent args)
        {
            SetCreamPied(entity, entity.Comp, false);
        }
    }
}
