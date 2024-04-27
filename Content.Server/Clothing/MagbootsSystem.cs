using Content.Server.Atmos.Components;
using Content.Shared.Alert;
using Content.Shared.Clothing;

namespace Content.Server.Clothing;

public sealed class MagbootsSystem : SharedMagbootsSystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MagbootsComponent, ClothingGotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<MagbootsComponent, ClothingGotUnequippedEvent>(OnGotUnequipped);
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

    private void OnGotUnequipped(EntityUid uid, MagbootsComponent component, ref ClothingGotUnequippedEvent args)
    {
        UpdateMagbootEffects(args.Wearer, uid, false, component);
    }

    private void OnGotEquipped(EntityUid uid, MagbootsComponent component, ref ClothingGotEquippedEvent args)
    {
        UpdateMagbootEffects(args.Wearer, uid, true, component);
    }
}
