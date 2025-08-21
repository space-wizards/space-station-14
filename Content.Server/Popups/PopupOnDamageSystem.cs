using Content.Shared.Damage;
using Robust.Shared.Timing;

namespace Content.Server.Popups;

public sealed class PopupOnDamageSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PopupOnDamageComponent, DamageChangedEvent>(OnDamage);
    }

    private void OnDamage(Entity<PopupOnDamageComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased)
            return;

        if (args.DamageDelta == null)
            return;

        if (ent.Comp.NextPopupTime > _gameTiming.CurTime)
            return;

        var showedPopup = false;

        foreach (var (type, _) in args.DamageDelta.DamageDict)
        {
            if (!ent.Comp.Popups.TryGetValue(type, out var data))
                continue;

            if (data.Threshold != null && (!args.Damageable.Damage.DamageDict.TryGetValue(type, out var total) || data.Threshold > total))
                continue;

            _popupSystem.PopupEntity(Loc.GetString(data.Popup), ent, ent, data.Type);
            showedPopup = true;
        }

        if (showedPopup)
            ent.Comp.NextPopupTime = _gameTiming.CurTime + ent.Comp.PopupsCooldown;
    }
}
