using Content.Server.Explosion.Components;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Nutrition.Components;
using Content.Server.Popups;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Interaction;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Rejuvenate;
using Content.Shared.Throwing;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Server.Nutrition.EntitySystems
{
    [UsedImplicitly]
    public sealed class CreamPieSystem : SharedCreamPieSystem
    {
        [Dependency] private readonly SolutionContainerSystem _solutions = default!;
        [Dependency] private readonly PuddleSystem _puddle = default!;
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
            _audio.PlayPvs(_audio.GetSound(creamPie.Sound), uid, AudioParams.Default.WithVariation(0.125f));

            if (EntityManager.TryGetComponent(uid, out FoodComponent? foodComp))
            {
                if (_solutions.TryGetSolution(uid, foodComp.Solution, out var solution))
                {
                    _puddle.TrySpillAt(uid, solution, out _, false);
                }
                if (!string.IsNullOrEmpty(foodComp.Trash))
                {
                    EntityManager.SpawnEntity(foodComp.Trash, Transform(uid).Coordinates);
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
                            timerTrigger.BeepSound);
                    }
                }
            }
        }

        protected override void CreamedEntity(EntityUid uid, CreamPiedComponent creamPied, ThrowHitByEvent args)
        {
            _popup.PopupEntity(Loc.GetString("cream-pied-component-on-hit-by-message", ("thrower", args.Thrown)), uid, args.Target);
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
