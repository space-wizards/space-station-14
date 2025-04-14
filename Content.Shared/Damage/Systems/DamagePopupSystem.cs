using System.Linq;
using Content.Shared.Damage.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;

namespace Content.Shared.Damage.Systems;

public sealed class DamagePopupSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

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

            _popupSystem.PopupPredicted(msg, uid, args.Origin);
        }
    }

    private void OnInteractHand(EntityUid uid, DamagePopupComponent component, InteractHandEvent args)
    {
        if (component.AllowTypeChange)
        {
            var next = (DamagePopupType) (((int)component.Type + 1) % Enum.GetValues<DamagePopupType>().Length);
            component.Type = next;
            Dirty(uid, component);
            _popupSystem.PopupPredicted("Target set to type: " + component.Type, uid, args.User);
        }
    }
}
