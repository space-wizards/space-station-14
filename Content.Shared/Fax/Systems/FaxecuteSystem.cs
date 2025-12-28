using Content.Shared.Damage.Systems;
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

    public void Faxecute(Entity<FaxMachineComponent> ent, DamageOnFaxecuteEvent? args = null)
    {
        var sendEntity = ent.Comp.PaperSlot.Item;
        if (sendEntity == null)
            return;

        if (!TryComp<FaxecuteComponent>(ent.Owner, out var faxecute))
            return;

        var damageSpec = faxecute.Damage;
        _damageable.TryChangeDamage(sendEntity.Value, damageSpec);
        _popupSystem.PopupEntity(Loc.GetString("fax-machine-popup-error", ("target", ent.Owner)), ent.Owner, PopupType.LargeCaution);
        return;

    }
}
