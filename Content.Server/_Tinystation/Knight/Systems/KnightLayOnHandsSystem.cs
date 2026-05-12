using Content.Shared._Tinystation.Knight.Components;
using Content.Shared._Tinystation.Knight.Events;
using Content.Server.Popups;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Player;

namespace Content.Server._Tinystation.Knight.Systems;

public sealed partial class KnightLayOnHandsSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<KnightLayOnHandsComponent, KnightLayOnHandsEvent>(OnLayOnHands);
    }

    private void OnLayOnHands(EntityUid uid, KnightLayOnHandsComponent component, KnightLayOnHandsEvent args)
    {
        var target = args.Target;
        var user = args.Performer;

        if (!_mobState.IsAlive(target))
        {
            _popup.PopupEntity(Loc.GetString(component.FailPopup), user, user, PopupType.SmallCaution);
            return;
        }

        if (!HasComp<DamageableComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString(component.FailPopup), user, user, PopupType.SmallCaution);
            return;
        }

        var healAmountPerType = 10f;

        var damage = new DamageSpecifier();
        damage.DamageDict.Add("Blunt", -healAmountPerType);
        damage.DamageDict.Add("Slash", -healAmountPerType);
        damage.DamageDict.Add("Piercing", -healAmountPerType);
        damage.DamageDict.Add("Heat", -healAmountPerType);
        damage.DamageDict.Add("Cold", -healAmountPerType);
        damage.DamageDict.Add("Bloodloss", -healAmountPerType);
        damage.DamageDict.Add("Shock", -healAmountPerType);
        damage.DamageDict.Add("Poison", -healAmountPerType);
        damage.DamageDict.Add("Radiation", -healAmountPerType);

        _damageable.TryChangeDamage(target, damage, true, origin: uid);

        _popup.PopupEntity(Loc.GetString(component.HealPopup, ("target", target)), user, user, PopupType.Large);
        _popup.PopupEntity(Loc.GetString(component.HealPopupOthers, ("user", user), ("target", target)), user, Filter.PvsExcept(user), true, PopupType.Medium);

        Spawn("HolyLightEffect", Transform(target).Coordinates);
        args.Handled = true;
    }
}
