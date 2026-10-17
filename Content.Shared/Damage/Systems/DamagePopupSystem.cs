using Content.Shared.Damage.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;

namespace Content.Shared.Damage.Systems;

/// <summary>
/// Handling displaying popups when an entity takes damage.
/// </summary>
/// <remarks>
/// Useful for training dummies, for example.
/// </remarks>
/// <seealso cref="DamagePopupComponent"/>
public sealed partial class DamagePopupSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SharedPopupSystem _popupSystem = default!;

    [Dependency] private EntityQuery<DamageableComponent> _damageableQuery;

    [SubscribeLocalEvent]
    private void OnDamageChange(Entity<DamagePopupComponent> ent, ref DamageDealtEvent args)
    {
        if (!_damageableQuery.TryComp(ent, out var damageable))
            return;

        var damageTotal = _damageable.GetTotalDamage((ent, damageable));
        var damageDelta = args.Total;

        var msg = ent.Comp.Type switch
        {
            DamagePopupType.Delta => damageDelta.ToString(),
            DamagePopupType.Total => damageTotal.ToString(),
            DamagePopupType.Combined => damageDelta + " | " + damageTotal,
            DamagePopupType.Hit => "!",
            _ => "Invalid type",
        };

        _popupSystem.PopupEntity(msg, ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnInteractHand(Entity<DamagePopupComponent> ent, ref InteractHandEvent args)
    {
        if (ent.Comp.AllowTypeChange)
        {
            var next = (DamagePopupType)(((int)ent.Comp.Type + 1) % Enum.GetValues<DamagePopupType>().Length);
            ent.Comp.Type = next;
            Dirty(ent);
            _popupSystem.PopupEntity(Loc.GetString("damage-popup-component-switched", ("setting", ent.Comp.Type)), ent.Owner);
        }
    }
}
