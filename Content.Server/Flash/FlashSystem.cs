using System.Linq;
using Content.Server.Flash.Components;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Light.EntitySystems;
using Content.Server.Popups;
using Content.Server.Stunnable;
using Content.Server.Mind;
using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Flash;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Traits.Assorted;
using Content.Shared.Weapons.Melee.Events;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Prototypes;
using InventoryComponent = Content.Shared.Inventory.InventoryComponent;
using Content.Server.Roles;
using Content.Shared.Roles;
using Content.Shared.Revolutionary;
using Content.Server.NPC.Systems;
using Content.Shared.Stunnable;
using Content.Server.Revolutionary;
using Content.Server.Chat.Managers;
using Content.Server.Mindshield.Components;

namespace Content.Server.Flash
{
    internal sealed class FlashSystem : SharedFlashSystem
    {
        [Dependency] private readonly AppearanceSystem _appearance = default!;
        [Dependency] private readonly AudioSystem _audio = default!;
        [Dependency] private readonly SharedChargesSystem _charges = default!;
        [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly SharedInteractionSystem _interaction = default!;
        [Dependency] private readonly InventorySystem _inventory = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly StunSystem _stun = default!;
        [Dependency] private readonly TagSystem _tag = default!;
        [Dependency] private readonly MindSystem _mind = default!;
        [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
        [Dependency] private readonly IPrototypeManager _prototype = default!;
        [Dependency] private readonly SharedStunSystem _sharedStun = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<FlashComponent, MeleeHitEvent>(OnFlashMeleeHit);
            // ran before toggling light for extra-bright lantern
            SubscribeLocalEvent<FlashComponent, UseInHandEvent>(OnFlashUseInHand, before: new []{ typeof(HandheldLightSystem) });

            SubscribeLocalEvent<InventoryComponent, FlashAttemptEvent>(OnInventoryFlashAttempt);

            SubscribeLocalEvent<FlashImmunityComponent, FlashAttemptEvent>(OnFlashImmunityFlashAttempt);
            SubscribeLocalEvent<PermanentBlindnessComponent, FlashAttemptEvent>(OnPermanentBlindnessFlashAttempt);
            SubscribeLocalEvent<TemporaryBlindnessComponent, FlashAttemptEvent>(OnTemporaryBlindnessFlashAttempt);
        }

        private void OnFlashMeleeHit(EntityUid uid, FlashComponent comp, MeleeHitEvent args)
        {
            if (!args.IsHit ||
                !args.HitEntities.Any() ||
                !UseFlash(uid, comp, args.User))
            {
                return;
            }

            args.Handled = true;
            foreach (var e in args.HitEntities)
            {
                Flash(e, args.User, uid, comp.FlashDuration, comp.SlowTo);
            }
        }

        private void OnFlashUseInHand(EntityUid uid, FlashComponent comp, UseInHandEvent args)
        {
            if (args.Handled || !UseFlash(uid, comp, args.User))
                return;

            args.Handled = true;
            FlashArea(uid, args.User, comp.Range, comp.AoeFlashDuration, comp.SlowTo, true);
        }

        private bool UseFlash(EntityUid uid, FlashComponent comp, EntityUid user)
        {
            if (comp.Flashing)
                return false;

            TryComp<LimitedChargesComponent>(uid, out var charges);
            if (_charges.IsEmpty(uid, charges))
                return false;

            _charges.UseCharge(uid, charges);
            _audio.PlayPvs(comp.Sound, uid);
            comp.Flashing = true;
            _appearance.SetData(uid, FlashVisuals.Flashing, true);

            if (_charges.IsEmpty(uid, charges))
            {
                _appearance.SetData(uid, FlashVisuals.Burnt, true);
                _tag.AddTag(uid, "Trash");
                _popup.PopupEntity(Loc.GetString("flash-component-becomes-empty"), user);
            }

            uid.SpawnTimer(400, () =>
            {
                _appearance.SetData(uid, FlashVisuals.Flashing, false);
                comp.Flashing = false;
            });

            return true;
        }

        public void Flash(EntityUid target, EntityUid? user, EntityUid? used, float flashDuration, float slowTo, bool displayPopup = true, FlashableComponent? flashable = null)
        {
            var stunTime = TimeSpan.FromSeconds(3);
            if (!Resolve(target, ref flashable, false)) return;

            var attempt = new FlashAttemptEvent(target, user, used);
            RaiseLocalEvent(target, attempt, true);

            if (attempt.Cancelled)
                return;

            flashable.LastFlash = _timing.CurTime;
            flashable.Duration = flashDuration / 1000f; // TODO: Make this sane...
            Dirty(flashable);

            _stun.TrySlowdown(target, TimeSpan.FromSeconds(flashDuration/1000f), true,
                slowTo, slowTo);

            //For Rev conversion (This probably shouldn't go in each and every flash but I'm honestly not sure where or how to put this somewhere else.)
            if (HasComp<HeadRevolutionaryComponent>(user) && !HasComp<RevolutionaryComponent>(target) && !HasComp<HeadRevolutionaryComponent>(target) &&
                !HasComp<MindShieldComponent>(target))
            {
                var mind = _mind.GetMind(target);
                if (mind != null && mind.OwnedEntity != null && used != null)
                {
                    _mind.AddRole(mind, new RevolutionaryRole(mind, _prototype.Index<AntagPrototype>("Rev")));
                    _npcFaction.RemoveFaction(mind.OwnedEntity.Value, "NanoTrasen");
                    _npcFaction.AddFaction(mind.OwnedEntity.Value, "Revolutionary");
                    AddComp<RevolutionaryRuleComponent>(mind.OwnedEntity.Value);
                    AddComp<RevolutionaryComponent>(mind.OwnedEntity.Value);
                    _charges.AddCharges(used.Value, 1);
                    _sharedStun.TryParalyze(mind.OwnedEntity.Value, stunTime, true);
                    if (mind.Session != null)
                    {
                        var message = Loc.GetString("rev-role-greeting");
                        var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", message));
                        _chatManager.ChatMessageToOne(Shared.Chat.ChatChannel.Server, message, wrappedMessage, default, false, mind.Session.ConnectedClient, Color.Red);
                    }
                }
            }

            if (displayPopup && user != null && target != user && EntityManager.EntityExists(user.Value))
            {
                user.Value.PopupMessage(target, Loc.GetString("flash-component-user-blinds-you",
                    ("user", Identity.Entity(user.Value, EntityManager))));
            }
        }

        public void OnPostFlash(EntityUid target, EntityUid user)
        {

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
                if (!_interaction.InRangeUnobstructed(entity, mapPosition, range, CollisionGroup.Opaque, (e) => flashableEntities.Contains(e) || e == source))
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
            foreach (var slot in new[] { "head", "eyes", "mask" })
            {
                if (args.Cancelled)
                    break;
                if (_inventory.TryGetSlotEntity(uid, slot, out var item, component))
                    RaiseLocalEvent(item.Value, args, true);
            }
        }

        private void OnFlashImmunityFlashAttempt(EntityUid uid, FlashImmunityComponent component, FlashAttemptEvent args)
        {
            if(component.Enabled)
                args.Cancel();
        }

        private void OnPermanentBlindnessFlashAttempt(EntityUid uid, PermanentBlindnessComponent component, FlashAttemptEvent args)
        {
            args.Cancel();
        }

        private void OnTemporaryBlindnessFlashAttempt(EntityUid uid, TemporaryBlindnessComponent component, FlashAttemptEvent args)
        {
            args.Cancel();
        }
    }

    public sealed class FlashAttemptEvent : CancellableEntityEventArgs
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
