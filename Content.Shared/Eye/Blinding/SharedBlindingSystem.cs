using Content.Shared.Clothing.Components;
using Content.Shared.Inventory.Events;
using Content.Shared.Inventory;
using Content.Shared.Item;
using JetBrains.Annotations;

namespace Content.Shared.Eye.Blinding
{
    public sealed class SharedBlindingSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<BlindfoldComponent, GotEquippedEvent>(OnEquipped);
            SubscribeLocalEvent<BlindfoldComponent, GotUnequippedEvent>(OnUnequipped);

            SubscribeLocalEvent<TemporaryBlindnessComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<TemporaryBlindnessComponent, ComponentShutdown>(OnShutdown);
        }

        private void OnEquipped(EntityUid uid, BlindfoldComponent component, GotEquippedEvent args)
        {
            if (!TryComp<SharedClothingComponent>(uid, out var clothing) || clothing.Slots == SlotFlags.PREVENTEQUIP) // we live in a society
                return;
            // Is the clothing in its actual slot?
            if (!clothing.Slots.HasFlag(args.SlotFlags))
                return;

            component.IsActive = true;
            if (!TryComp<BlindableComponent>(args.Equipee, out var blindComp))
                return;
            AdjustBlindSources(args.Equipee, true, blindComp);
        }

        private void OnUnequipped(EntityUid uid, BlindfoldComponent component, GotUnequippedEvent args)
        {
            if (!component.IsActive)
                return;
            component.IsActive = false;
            if (!TryComp<BlindableComponent>(args.Equipee, out var blindComp))
                return;
            AdjustBlindSources(args.Equipee, false, blindComp);
        }

        private void OnInit(EntityUid uid, TemporaryBlindnessComponent component, ComponentInit args)
        {
            AdjustBlindSources(uid, true);
        }

        private void OnShutdown(EntityUid uid, TemporaryBlindnessComponent component, ComponentShutdown args)
        {
            AdjustBlindSources(uid, false);
        }

        [PublicAPI]
        public void AdjustBlindSources(EntityUid uid, bool Add, BlindableComponent? blindable = null)
        {
            if (!Resolve(uid, ref blindable, false))
                return;

            if (Add)
            {
                blindable.Sources++;
            } else
            {
                blindable.Sources--;
            }
        }

        public void AdjustEyeDamage(EntityUid uid, bool Add, BlindableComponent? blindable = null)
        {
            if (!Resolve(uid, ref blindable, false))
                return;

            if (Add)
            {
                blindable.EyeDamage++;
            } else
            {
                blindable.EyeDamage--;
            }

            if (!blindable.EyeTooDamaged && blindable.EyeDamage == 8)
                AdjustBlindSources(uid, true, blindable);

            if (blindable.EyeTooDamaged && blindable.EyeDamage < 8)
                AdjustBlindSources(uid, false, blindable);
        }
    }
}
