using System;
using Content.Server.Flash.Components;
using Content.Server.Inventory.Components;
using Content.Server.Items;
using Content.Server.Stunnable;
using Content.Server.Stunnable.Components;
using Content.Server.Weapon.Melee;
using Content.Shared.Examine;
using Content.Shared.Flash;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Inventory;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Sound;
using Content.Shared.Stunnable;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.Flash
{
    internal sealed class FlashSystem : SharedFlashSystem
    {
        [Dependency] private readonly IEntityLookup _entityLookup = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly StunSystem _stunSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<FlashComponent, MeleeHitEvent>(OnFlashMeleeHit);
            SubscribeLocalEvent<FlashComponent, MeleeInteractEvent>(OnFlashMeleeInteract);
            SubscribeLocalEvent<FlashComponent, UseInHandEvent>(OnFlashUseInHand);
            SubscribeLocalEvent<FlashComponent, ExaminedEvent>(OnFlashExamined);
            SubscribeLocalEvent<InventoryComponent, FlashAttemptEvent>(OnInventoryFlashAttempt);
            SubscribeLocalEvent<FlashImmunityComponent, FlashAttemptEvent>(OnFlashImmunityFlashAttempt);
        }

        private void OnFlashMeleeHit(EntityUid uid, FlashComponent comp, MeleeHitEvent args)
        {
            if (!UseFlash(comp, args.User))
            {
                return;
            }

            args.Handled = true;
            foreach (IEntity e in args.HitEntities)
            {
                Flash(e.Uid, args.User.Uid, uid, comp.FlashDuration, comp.SlowTo);
            }
        }

        private void OnFlashMeleeInteract(EntityUid uid, FlashComponent comp, MeleeInteractEvent args)
        {
            if (!UseFlash(comp, args.User))
            {
                return;
            }

            if (args.Entity.HasComponent<FlashableComponent>())
            {
                args.CanInteract = true;
                Flash(args.Entity.Uid, args.User.Uid, uid, comp.FlashDuration, comp.SlowTo);
            }
        }

        private void OnFlashUseInHand(EntityUid uid, FlashComponent comp, UseInHandEvent args)
        {
            if (!UseFlash(comp, args.User))
            {
                return;
            }

            foreach (var entity in _entityLookup.GetEntitiesInRange(comp.Owner.Transform.Coordinates, comp.Range))
            {
                Flash(entity.Uid, args.User.Uid, uid, comp.AoeFlashDuration, comp.SlowTo);
            }
        }

        private bool UseFlash(FlashComponent comp, IEntity user)
        {
            if (comp.HasUses)
            {
                // TODO flash visualizer
                if (!comp.Owner.TryGetComponent<SpriteComponent>(out var sprite))
                    return false;

                if (--comp.Uses == 0)
                {
                    sprite.LayerSetState(0, "burnt");
                    comp.Owner.PopupMessage(user, Loc.GetString("flash-component-becomes-empty"));
                }
                else if (!comp.Flashing)
                {
                    int animLayer = sprite.AddLayerWithState("flashing");
                    comp.Flashing = true;

                    comp.Owner.SpawnTimer(400, () =>
                    {
                        sprite.RemoveLayer(animLayer);
                        comp.Flashing = false;
                    });
                }

                SoundSystem.Play(Filter.Pvs(comp.Owner), comp.Sound.GetSound(), comp.Owner, AudioParams.Default);

                return true;
            }

            return false;
        }

        public void Flash(EntityUid target, EntityUid? user, EntityUid? used, float flashDuration, float slowTo, bool displayPopup = true)
        {
            var attempt = new FlashAttemptEvent(target, user, used);
            RaiseLocalEvent(target, attempt);

            if (attempt.Cancelled)
                return;

            if (EntityManager.TryGetComponent<FlashableComponent>(target, out var flashable))
            {
                flashable.LastFlash = _gameTiming.CurTime;
                flashable.Duration = flashDuration / 1000f; // TODO: Make this sane...
                flashable.Dirty();
            }

            _stunSystem.TrySlowdown(target, TimeSpan.FromSeconds(flashDuration/1000f),
                slowTo, slowTo);

            if (displayPopup && user != null && target != user)
            {
                // TODO Resolving the IEntity here bad.
                if(EntityManager.TryGetEntity(user.Value, out var userEntity)
                && EntityManager.TryGetEntity(target, out var targetEntity))

                userEntity.PopupMessage(targetEntity,
                    Loc.GetString(
                        "flash-component-user-blinds-you",
                        ("user", userEntity)
                    )
                );
            }
        }

        public void FlashArea(EntityUid source, EntityUid? user, float range, float duration, float slowTo = 0f, bool displayPopup = false, SoundSpecifier? sound = null)
        {
            var transform = EntityManager.GetComponent<ITransformComponent>(source);

            foreach (var entity in _entityLookup.GetEntitiesInRange(transform.Coordinates, range))
            {
                if (!entity.HasComponent<FlashableComponent>() ||
                    !transform.InRangeUnobstructed(entity, range, CollisionGroup.Opaque)) continue;

                Flash(entity.Uid, user, source, duration, slowTo, displayPopup);
            }

            if (sound != null)
            {
                SoundSystem.Play(Filter.Pvs(transform), sound.GetSound(), transform.Coordinates);
            }
        }

        private void OnFlashExamined(EntityUid uid, FlashComponent comp, ExaminedEvent args)
        {
            if (!comp.HasUses)
            {
                args.PushText(Loc.GetString("flash-component-examine-empty"));
                return;
            }

            if (args.IsInDetailsRange)
            {
                args.PushMarkup(
                    Loc.GetString(
                        "flash-component-examine-detail-count",
                        ("count", comp.Uses),
                        ("markupCountColor", "green")
                    )
                );
            }
        }

        private void OnInventoryFlashAttempt(EntityUid uid, InventoryComponent component, FlashAttemptEvent args)
        {
            // Forward the event to the glasses, if any.
            if(component.TryGetSlotItem(EquipmentSlotDefines.Slots.EYES, out ItemComponent? glasses))
                RaiseLocalEvent(glasses.Owner.Uid, args);
        }

        private void OnFlashImmunityFlashAttempt(EntityUid uid, FlashImmunityComponent component, FlashAttemptEvent args)
        {
            if(component.Enabled)
                args.Cancel();
        }
    }

    public class FlashAttemptEvent : CancellableEntityEventArgs
    {
        public readonly EntityUid Target;
        public readonly EntityUid? User;
        public readonly EntityUid? Used;

        public FlashAttemptEvent(EntityUid target, EntityUid? user, EntityUid? used)
        {
            Target = target;
            User = user;
            Used = used;
        }
    }
}
