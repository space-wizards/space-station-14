using Content.Server.Atmos.Components;
using Content.Shared.Alert;
using Content.Shared.Clothing;
using Content.Shared.Inventory.Events;

namespace Content.Server.Clothing;

public sealed class MagbootsSystem : SharedMagbootsSystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MagbootsComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<MagbootsComponent, GotUnequippedEvent>(OnGotUnequipped);
    }

    protected override void UpdateMagbootEffects(EntityUid parent, EntityUid uid, bool state, MagbootsComponent? component)
    {
        if (!Resolve(uid, ref component))
            return;
        state = state && component.On;

        if (TryComp(parent, out MovedByPressureComponent? movedByPressure))
        {
            movedByPressure.Enabled = !state;
        }

        if (state)
        {
            _alerts.ShowAlert(parent, AlertType.Magboots);
        }
        else
        {
            _alerts.ClearAlert(parent, AlertType.Magboots);
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
