using Content.Server.Audio;
using Content.Server.Power.Components;
using Content.Server.Recycling.Components;
using Content.Shared.Audio;
using Content.Shared.Body.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Recycling;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.Recycling
{
    public sealed class RecyclerSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly AmbientSoundSystem _ambience = default!;
        [Dependency] private readonly TagSystem _tags = default!;

        private const float RecyclerSoundCooldown = 0.8f;

        public override void Initialize()
        {
            SubscribeLocalEvent<RecyclerComponent, StartCollideEvent>(OnCollide);
            SubscribeLocalEvent<RecyclerComponent, GotEmaggedEvent>(OnEmagged);
        }

        public void EnableRecycler(RecyclerComponent component)
        {
            if (component.Enabled) return;

            component.Enabled = true;
            _ambience.SetAmbience(component.Owner, true);
        }

        public void DisableRecycler(RecyclerComponent component)
        {
            if (!component.Enabled) return;

            component.Enabled = false;
            _ambience.SetAmbience(component.Owner, false);
        }

        private void OnCollide(EntityUid uid, RecyclerComponent component, StartCollideEvent args)
        {
            if (component.Enabled && args.OurFixture.ID != "brrt") return;

            Recycle(component, args.OtherFixture.Body.Owner);
        }

        private void Recycle(RecyclerComponent component, EntityUid entity)
        {
            RecyclableComponent? recyclable = null;

            // Can only recycle things that are recyclable... And also check the safety of the thing to recycle.
            if (!_tags.HasTag(entity, "Recyclable") &&
                (!TryComp(entity, out recyclable) || !recyclable.Safe && component.Safe))
            {
                return;
            }

            // TODO: Prevent collision with recycled items

            // Mobs are a special case!
            if (CanGib(component, entity))
            {
                Comp<SharedBodyComponent>(entity).Gib(true);
                Bloodstain(component);
                return;
            }

            if (recyclable == null)
                QueueDel(entity);
            else
                Recycle(recyclable, component.Efficiency);

            if (component.Sound != null && (_timing.CurTime - component.LastSound).TotalSeconds > RecyclerSoundCooldown)
            {
                SoundSystem.Play(Filter.Pvs(component.Owner, entityManager: EntityManager), component.Sound.GetSound(), component.Owner, AudioHelpers.WithVariation(0.01f).WithVolume(-3));
                component.LastSound = _timing.CurTime;
            }
        }

        private bool CanGib(RecyclerComponent component, EntityUid entity)
        {
            // TODO: Power needs a helper for this jeez
            return HasComp<SharedBodyComponent>(entity) && !component.Safe &&
                   TryComp<ApcPowerReceiverComponent>(component.Owner, out var receiver) && receiver.Powered;
        }

        public void Bloodstain(RecyclerComponent component)
        {
            if (EntityManager.TryGetComponent(component.Owner, out AppearanceComponent? appearance))
            {
                appearance.SetData(RecyclerVisuals.Bloody, true);
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

        private void OnEmagged(EntityUid uid, RecyclerComponent component, GotEmaggedEvent args)
        {
            if (!component.Safe) return;
            component.Safe = false;
            args.Handled = true;
        }
    }
}
