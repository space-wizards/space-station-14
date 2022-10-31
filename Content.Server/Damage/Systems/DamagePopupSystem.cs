using Content.Server.Damage.Components;
using Content.Server.Popups;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Player;

namespace Content.Server.Damage.Systems;

public sealed class DamagePopupSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DamagePopupComponent, DamageChangedEvent>(OnDamageChange);
    }

    private void OnDamageChange(EntityUid uid, DamagePopupComponent component, DamageChangedEvent args)
    {
        if (args.DamageIncreased && args.DamageDelta != null)
        {
            string msg = ""; // Stores the text to be shown in the popup message
            FixedPoint2 damageTotal = args.Damageable.TotalDamage;
            FixedPoint2 damageDelta = args.DamageDelta.Total;

            switch(component.DamagePopupTypeString)
            {
                case "damageDelta":
                    msg = damageDelta.ToString();
                    break;
                case "damageTotal":
                    msg = damageTotal.ToString();
                    break;
                case "damageCombined":
                    msg = damageDelta + " | " + damageTotal;
                    break;
                default:
                    msg = "Invalid type";
                    break;
            }

            _popupSystem.PopupEntity(msg, uid, Filter.Pvs(uid, 2F, EntityManager));
        }
    }
}
