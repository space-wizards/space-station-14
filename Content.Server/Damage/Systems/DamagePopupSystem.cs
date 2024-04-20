using System.Linq;
using Content.Server.Damage.Components;
using Content.Server.Popups;
using Content.Shared.Damage;
using Content.Shared.Interaction;

namespace Content.Server.Damage.Systems;

public sealed class DamagePopupSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DamagePopupComponent, DamageChangedEvent>(OnDamageChange);
        SubscribeLocalEvent<DamagePopupComponent, InteractHandEvent>(OnInteractHand);
    }

    private void OnDamageChange(EntityUid uid, DamagePopupComponent component, DamageChangedEvent args)
    {
        if (args.DamageDelta != null)
        {
            var damageTotal = args.Damageable.TotalDamage;
            var damageDelta = args.DamageDelta.GetTotal();

            var msg = component.Type switch
            {
                DamagePopupType.Delta => damageDelta.ToString(),
                DamagePopupType.Total => damageTotal.ToString(),
                DamagePopupType.Combined => damageDelta + " | " + damageTotal,
                DamagePopupType.Hit => "!",
                _ => "Invalid type",
            };
            _popupSystem.PopupEntity(msg, uid);
        }
    }

    private void OnInteractHand(EntityUid uid, DamagePopupComponent component, InteractHandEvent args)
    {
        if (component.AllowTypeChange)
        {
            if (component.Type == Enum.GetValues(typeof(DamagePopupType)).Cast<DamagePopupType>().Last())
            {
                component.Type = Enum.GetValues(typeof(DamagePopupType)).Cast<DamagePopupType>().First();
            }
            else
            {
                component.Type = (DamagePopupType) (int) component.Type + 1;
            }
            _popupSystem.PopupEntity("Target set to type: " + component.Type.ToString(), uid);
        }
    }
}
