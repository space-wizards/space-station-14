using Content.Server.Audio;
using Content.Server.Body.Systems;
using Content.Server.GameTicking;
using Content.Server.Players;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Recycling.Components;
using Content.Shared.Audio;
using Content.Shared.Body.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Events;
using Content.Shared.Recycling;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.Recycling
{
    public sealed class RecyclerSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly AmbientSoundSystem _ambience = default!;
        [Dependency] private readonly BodySystem _bodySystem = default!;
        [Dependency] private readonly GameTicker _ticker = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly TagSystem _tags = default!;
        [Dependency] private readonly AudioSystem _soundSystem = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;

        private const string RecyclerColliderName = "brrt";

        private const float RecyclerSoundCooldown = 0.8f;

        public override void Initialize()
        {
            SubscribeLocalEvent<RecyclerComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<RecyclerComponent, StartCollideEvent>(OnCollide);
            SubscribeLocalEvent<RecyclerComponent, GotEmaggedEvent>(OnEmagged);
            SubscribeLocalEvent<RecyclerComponent, SuicideEvent>(OnSuicide);
            SubscribeLocalEvent<RecyclerComponent, PowerChangedEvent>(OnPowerChanged);
        }

        private void OnExamined(EntityUid uid, RecyclerComponent component, ExaminedEvent args)
        {
            args.PushMarkup(Loc.GetString("recycler-count-items", ("items", component.ItemsProcessed)));
        }

        private void OnSuicide(EntityUid uid, RecyclerComponent component, SuicideEvent args)
        {
            if (args.Handled) return;
            args.SetHandled(SuicideKind.Bloodloss);
            var victim = args.Victim;
            if (TryComp(victim, out ActorComponent? actor) &&
                actor.PlayerSession.ContentData()?.Mind is { } mind)
            {
                _ticker.OnGhostAttempt(mind, false);
                if (mind.OwnedEntity is { Valid: true } entity)
                {
                    _popup.PopupEntity(Loc.GetString("recycler-component-suicide-message"), entity);
                }
            }

            _popup.PopupEntity(Loc.GetString("recycler-component-suicide-message-others", ("victim", Identity.Entity(victim, EntityManager))),
                victim,
                Filter.PvsExcept(victim, entityManager: EntityManager), true);

            if (TryComp<BodyComponent?>(victim, out var body))
            {
                _bodySystem.GibBody(victim, true, body);
            }

            Bloodstain(component);
        }

        public void EnableRecycler(RecyclerComponent component)
        {
            if (component.Enabled) return;

            component.Enabled = true;

            if (TryComp(component.Owner, out ApcPowerReceiverComponent? apcPower))
            {
                _ambience.SetAmbience(component.Owner, apcPower.Powered);
            }
            else
            {
                _ambience.SetAmbience(component.Owner, true);
            }

        }

        public void DisableRecycler(RecyclerComponent component)
        {
            if (!component.Enabled) return;

            component.Enabled = false;
            _ambience.SetAmbience(component.Owner, false);
        }

        private void OnPowerChanged(EntityUid uid, RecyclerComponent component, ref PowerChangedEvent args)
        {
            if (component.Enabled)
            {
                _ambience.SetAmbience(uid, args.Powered);
            }
        }

        private void OnCollide(EntityUid uid, RecyclerComponent component, ref StartCollideEvent args)
        {
            if (component.Enabled && args.OurFixture.ID != RecyclerColliderName)
                return;

            if (TryComp(uid, out ApcPowerReceiverComponent? apcPower))
            {
                if (!apcPower.Powered)
                    return;
            }

            Recycle(component, args.OtherFixture.Body.Owner);
        }

        private void Recycle(RecyclerComponent component, EntityUid entity)
        {
            RecyclableComponent? recyclable = null;

            // Can only recycle things that are tagged trash or recyclable... And also check the safety of the thing to recycle.
            if (!_tags.HasAnyTag(entity, "Trash", "Recyclable") &&
                (!TryComp(entity, out recyclable) || !recyclable.Safe && component.Safe))
            {
                return;
            }

            // TODO: Prevent collision with recycled items

            // Mobs are a special case!
            if (CanGib(component, entity))
            {
                _bodySystem.GibBody(entity, true, Comp<BodyComponent>(entity));
                Bloodstain(component);
                return;
            }

            if (recyclable == null)
                QueueDel(entity);
            else
                Recycle(recyclable, component.Efficiency);

            if (component.Sound != null && (_timing.CurTime - component.LastSound).TotalSeconds > RecyclerSoundCooldown)
            {
                _soundSystem.PlayPvs(component.Sound, component.Owner, AudioHelpers.WithVariation(0.01f).WithVolume(-3));
                component.LastSound = _timing.CurTime;
            }

            component.ItemsProcessed++;
        }

        private bool CanGib(RecyclerComponent component, EntityUid entity)
        {
            return HasComp<BodyComponent>(entity) && !component.Safe &&
                   this.IsPowered(component.Owner, EntityManager);
        }

        public void Bloodstain(RecyclerComponent component)
        {
            if (EntityManager.TryGetComponent(component.Owner, out AppearanceComponent? appearance))
            {
                _appearanceSystem.SetData(component.Owner, RecyclerVisuals.Bloody, true, appearance);
            }
        }

        private void Recycle(RecyclableComponent component, float efficiency = 1f)
        {
            if (!string.IsNullOrEmpty(component.Prototype))
            {
                var xform = Transform(component.Owner);

                for (var i = 0; i < Math.Max(component.Amount * efficiency, 1); i++)
                {
                    Spawn(component.Prototype, xform.Coordinates);
                }
            }

            QueueDel(component.Owner);
        }

        private void OnEmagged(EntityUid uid, RecyclerComponent component, ref GotEmaggedEvent args)
        {
            if (!component.Safe) return;
            component.Safe = false;
            args.Handled = true;
        }
    }
}
