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
        }

        private void OnEquipped(EntityUid uid, BlindfoldComponent component, GotEquippedEvent args)
        {
            if (!TryComp<SharedItemComponent>(uid, out var clothing) || clothing.SlotFlags == SlotFlags.PREVENTEQUIP) // we live in a society
                return;
            // Is the clothing in its actual slot?
            if (!clothing.SlotFlags.HasFlag(args.SlotFlags))
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
    }
}
