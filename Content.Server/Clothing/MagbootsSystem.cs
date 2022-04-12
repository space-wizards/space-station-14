using Content.Server.Atmos.Components;
using Content.Server.Clothing.Components;
using Content.Shared.Alert;
using Content.Shared.Clothing;
using Content.Shared.Inventory.Events;

namespace Content.Server.Clothing
{
    public sealed class MagbootsSystem : SharedMagbootsSystem
    {
        [Dependency] private readonly AlertsSystem _alertsSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<MagbootsComponent, GotEquippedEvent>(OnGotEquipped);
            SubscribeLocalEvent<MagbootsComponent, GotUnequippedEvent>(OnGotUnequipped);
        }

        public void UpdateMagbootEffects(EntityUid parent, EntityUid uid, bool state, MagbootsComponent? component)
        {
            if (!Resolve(uid, ref component))
                return;
            state = state && component.On;

            if (TryComp(parent, out MovedByPressureComponent? movedByPressure))
            {
                movedByPressure.Enabled = state;
            }

            if (state)
            {
                _alertsSystem.ShowAlert(parent, AlertType.Magboots);
            }
            else
            {
                _alertsSystem.ClearAlert(parent, AlertType.Magboots);
            }
        }

        private void OnGotUnequipped(EntityUid uid, MagbootsComponent component, GotUnequippedEvent args)
        {
            if (args.Slot == "shoes")
            {
                UpdateMagbootEffects(args.Equipee, uid, false, component);
            }
        }

        private void OnGotEquipped(EntityUid uid, MagbootsComponent component, GotEquippedEvent args)
        {
            if (args.Slot == "shoes")
            {
                UpdateMagbootEffects(args.Equipee, uid, true, component);
            }
        }
    }
}
