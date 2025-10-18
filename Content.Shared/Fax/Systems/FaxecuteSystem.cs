using Content.Shared.Damage;
using Content.Shared.Popups;
using Content.Shared.Fax.Components;

namespace Content.Shared.Fax.Systems;
/// <summary>
/// System for handling execution of a mob within fax when copy or send attempt is made.
/// </summary>
public sealed class FaxecuteSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    public void Faxecute(EntityUid uid, FaxMachineComponent component, DamageOnFaxecuteEvent? args = null)
    {
        var sendEntity = component.PaperSlot.Item;
        if (sendEntity == null)
            return;

        if (!TryComp<FaxecuteComponent>(uid, out var faxecute))
            return;

        var damageSpec = faxecute.Damage;
        _damageable.TryChangeDamage(sendEntity, damageSpec);
        _popupSystem.PopupEntity(Loc.GetString("fax-machine-popup-error", ("target", uid)), uid, PopupType.LargeCaution);
        return;

    }
}
