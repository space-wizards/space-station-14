using Content.Server.Chemistry.EntitySystems;
using Content.Server.Explosion.Components;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Nutrition.Components;
using Content.Server.Popups;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Interaction;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Rejuvenate;
using Content.Shared.Throwing;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server.Nutrition.EntitySystems
{
    [UsedImplicitly]
    public sealed class CreamPieSystem : SharedCreamPieSystem
    {
        [Dependency] private readonly SolutionContainerSystem _solutions = default!;
        [Dependency] private readonly SpillableSystem _spillable = default!;
        [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
        [Dependency] private readonly TriggerSystem _trigger = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly PopupSystem _popup = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<CreamPieComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<CreamPiedComponent, RejuvenateEvent>(OnRejuvenate);
        }

        protected override void SplattedCreamPie(EntityUid uid, CreamPieComponent creamPie)
        {
            _audio.Play(_audio.GetSound(creamPie.Sound), Filter.Pvs(uid), uid, false, new AudioParams().WithVariation(0.125f));

            if (EntityManager.TryGetComponent<FoodComponent?>(uid, out var foodComp))
            {
                if (_solutions.TryGetSolution(uid, foodComp.SolutionName, out var solution))
                {
                    _spillable.SpillAt(uid, solution, "PuddleSmear", false);
                }
                if (!string.IsNullOrEmpty(foodComp.TrashPrototype))
                {
                    EntityManager.SpawnEntity(foodComp.TrashPrototype, Transform(uid).Coordinates);
                }
            }
            ActivatePayload(uid);

            EntityManager.QueueDeleteEntity(uid);
        }

        private void OnInteractUsing(EntityUid uid, CreamPieComponent component, InteractUsingEvent args)
        {
            ActivatePayload(uid);
        }

        private void ActivatePayload(EntityUid uid)
        {
            if (_itemSlots.TryGetSlot(uid, CreamPieComponent.PayloadSlotName, out var itemSlot)) 
            {
                if (_itemSlots.TryEject(uid, itemSlot, user: null, out var item)) 
                {
                    if (TryComp<OnUseTimerTriggerComponent>(item.Value, out var timerTrigger))
                    {
                        _trigger.HandleTimerTrigger(
                            item.Value,
                            null,
                            timerTrigger.Delay,
                            timerTrigger.BeepInterval,
                            timerTrigger.InitialBeepDelay,
                            timerTrigger.BeepSound,
                            timerTrigger.BeepParams);
                    }
                }
            }
        }

        protected override void CreamedEntity(EntityUid uid, CreamPiedComponent creamPied, ThrowHitByEvent args)
        {
            _popup.PopupEntity(Loc.GetString("cream-pied-component-on-hit-by-message",("thrower", args.Thrown)), uid, args.Target);
            var otherPlayers = Filter.Empty().AddPlayersByPvs(uid);
            if (TryComp<ActorComponent>(args.Target, out var actor)) 
            {
                otherPlayers.RemovePlayer(actor.PlayerSession);
            }
            _popup.PopupEntity(Loc.GetString("cream-pied-component-on-hit-by-message-others", ("owner", uid),("thrower", args.Thrown)), uid, otherPlayers, false);
        }

        private void OnRejuvenate(EntityUid uid, CreamPiedComponent component, RejuvenateEvent args)
        {
            SetCreamPied(uid, component, false);
        }
    }
}
