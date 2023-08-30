using Content.Server.Damage.Components;
using Content.Server.Popups;
using Content.Shared.Damage;
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
        if (args.DamageDelta != null)
        {
            var damageTotal = args.Damageable.TotalDamage;
            var damageDelta = args.DamageDelta.Total;

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
}
