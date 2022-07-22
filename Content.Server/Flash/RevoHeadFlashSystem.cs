using Content.Server.Flash.Components;
using Content.Server.Light.EntitySystems;
using Content.Server.Stunnable;
using Content.Server.Weapon.Melee;
using Content.Shared.Flash;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Physics;
using Content.Server.Popups;
using Content.Shared.Roles;
using Content.Shared.Sound;
using Content.Server.Mind.Components;
using Content.Server.Traitor;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using InventoryComponent = Content.Shared.Inventory.InventoryComponent;

namespace Content.Server.Flash
{
    internal sealed class RevoHeadFlashSystem : SharedFlashSystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly StunSystem _stunSystem = default!;
        [Dependency] private readonly InventorySystem _inventorySystem = default!;
        [Dependency] private readonly MetaDataSystem _metaSystem = default!;
        [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;

        private const string RevolutionaryPrototypeId = "Revolutionary";

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<FlashEvent>(RevoFlash);
        }

        private void OnFlashMeleeHit(EntityUid uid, RevoFlashComponent flashcomp, MeleeHitEvent args)
        {
            if (!UseFlash(flashcomp, args.User))
                return;

            args.Handled = true;
            foreach (var entity in args.HitEntities)
            {
                Flash(entity, args.User, uid, flashcomp.FlashDuration, flashcomp.SlowTo);
            }
        }
        
        private void OnFlashUseInHand(EntityUid uid, RevoFlashComponent comp, UseInHandEvent args)
        {
            if (args.Handled || !UseFlash(comp, args.User))
                return;

            args.Handled = true;
            FlashArea(uid, args.User, comp.Range, comp.AoeFlashDuration, comp.SlowTo, true);
        }

        private bool UseFlash(RevoFlashComponent flashcomp, EntityUid user)
        {
            if (!EntityManager.TryGetComponent<SpriteComponent?>(flashcomp.Owner, out var sprite))
                return false;

            else if (!flashcomp.Flashing)
            {
                int animLayer = sprite.AddLayerWithState("flashing");
                flashcomp.Flashing = true;

                flashcomp.Owner.SpawnTimer(400, () =>
                {
                    sprite.RemoveLayer(animLayer);
                    flashcomp.Flashing = false;
                });
            }

                SoundSystem.Play(flashcomp.Sound.GetSound(), Filter.Pvs(flashcomp.Owner), flashcomp.Owner, AudioParams.Default);

                return true;
        }

        public void RevoFlash(EntityUid target, EntityUid? user, float flashDuration, float slowTo, FlashableComponent? flashable = null)
        {
            _stunSystem.TrySlowdown(target, TimeSpan.FromSeconds(flashDuration/1000f), true,
                slowTo, slowTo);

            if (!TryComp<MindComponent>(target, out MindComponent? usermindcomp) || usermindcomp is null || usermindcomp.Mind is null) return;

            // Lord above forgive me, for I have sinned
            foreach (var role in usermindcomp.Mind.AllRoles)
            {
                // If the user has the revo head role they can use this flash to convert ppl
                if (role.Name == "Revolutionary Head")
                {
                    if (!TryComp<MindComponent>(target, out MindComponent? targetmindcomp) || targetmindcomp is null || targetmindcomp.Mind is null) return;
                    var antagPrototype = _prototypeManager.Index<AntagPrototype>(RevolutionaryPrototypeId);
                    var revoRole = new RevoRole(targetmindcomp.Mind, antagPrototype);
                    targetmindcomp.Mind.AddRole(revoRole);
                }
            }
        }

        public void FlashArea(EntityUid source, EntityUid? user, float range, float duration, float slowTo = 0.8f, bool displayPopup = false, SoundSpecifier? sound = null)
        {
            var transform = EntityManager.GetComponent<TransformComponent>(source);
            var mapPosition = transform.MapPosition;
            var flashableEntities = new List<EntityUid>();
            var flashableQuery = GetEntityQuery<FlashableComponent>();

            foreach (var entity in _entityLookup.GetEntitiesInRange(transform.Coordinates, range))
            {
                if (!flashableQuery.HasComponent(entity))
                    continue;

                flashableEntities.Add(entity);
            }

            foreach (var entity in flashableEntities)
            {
                // Check for unobstructed entities while ignoring the mobs with flashable components.
                if (!_interactionSystem.InRangeUnobstructed(entity, mapPosition, range, CollisionGroup.Opaque, (e) => flashableEntities.Contains(e)))
                    continue;

                // They shouldn't have flash removed in between right?
                Flash(entity, user, source, duration, slowTo, displayPopup, flashableQuery.GetComponent(entity));
            }
            if (sound != null)
            {
                SoundSystem.Play(sound.GetSound(), Filter.Pvs(transform), source);
            }
        }

        private void OnInventoryFlashAttempt(EntityUid uid, InventoryComponent component, FlashAttemptEvent args)
        {
            foreach (var slot in new string[]{"head", "eyes", "mask"})
            {
                if (args.Cancelled)
                    break;
                if (_inventorySystem.TryGetSlotEntity(uid, slot, out var item, component))
                    RaiseLocalEvent(item.Value, args, true);
            }
        }

        private void OnFlashImmunityFlashAttempt(EntityUid uid, FlashImmunityComponent component, FlashAttemptEvent args)
        {
            if(component.Enabled)
                args.Cancel();
        }
    }

    public sealed class RevoFlashAttemptEvent : CancellableEntityEventArgs
    {
        public readonly EntityUid Target;
        public readonly EntityUid? User;
        public readonly EntityUid? Used;

        public RevoFlashAttemptEvent(EntityUid target, EntityUid? user, EntityUid? used)
        {
            Target = target;
            User = user;
            Used = used;
        }
    }
}
