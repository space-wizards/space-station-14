using Content.Shared.Popups;
using Content.Shared.Damage;
using Content.Shared.MobState.Components;
using Content.Server.DoAfter;
using Content.Shared.Revenant;
using Robust.Shared.Random;
using Robust.Shared.Player;

namespace Content.Server.Revenant.EntitySystems;

public sealed partial class RevenantSystem : EntitySystem
{
    public void BeginSoulSearchDoAfter(EntityUid uid, EntityUid target, RevenantComponent revenant, EssenceComponent essence)
    {
        _popup.PopupEntity(Loc.GetString("revenant-soul-searching", ("target", target)), uid, Filter.Entities(uid), PopupType.Medium);
        var searchDoAfter = new DoAfterEventArgs(uid, revenant.SoulSearchDuration, target: target)
        {
            BreakOnUserMove = true,
            UserFinishedEvent = new SoulSearchDoAfterComplete(target),
        };
        _doAfter.DoAfter(searchDoAfter);
    }

    private void OnSoulSearchComplete(EntityUid uid, RevenantComponent component, SoulSearchDoAfterComplete args)
    {
        if (!TryComp<EssenceComponent>(args.Target, out var essence))
            return;
        essence.SearchComplete = true;

        string message;
        switch (essence.EssenceAmount)
        {
            case <= 30:
                message = "revenant-soul-yield-low";
                break;
            case >= 50:
                message = "revenant-soul-yield-high";
                break;
            default:
                message = "revenant-soul-yield-average";
                break;
        }
        _popup.PopupEntity(Loc.GetString(message, ("target", args.Target)), args.Target, Filter.Entities(uid), PopupType.Medium);
    }

    public void BeginHarvestDoAfter(EntityUid uid, EntityUid target, RevenantComponent revenant, EssenceComponent essence)
    {
        if (essence.Harvested)
        {
            _popup.PopupEntity(Loc.GetString("revenant-soul-harvested"), target, Filter.Entities(uid), PopupType.SmallCaution);
            return;
        }

        revenant.HarvestCancelToken = new();
        var doAfter = new DoAfterEventArgs(uid, revenant.HarvestDuration, revenant.HarvestCancelToken.Token, target)
        {
            BreakOnDamage = true,
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            NeedHand = false,
            UserFinishedEvent = new HarvestDoAfterComplete(target),
            UserCancelledEvent = new HarvestDoAfterCancelled(),
        };

        if (TryComp<AppearanceComponent>(uid, out var app))
            app.SetData(RevenantVisuals.Harvesting, true);

        _popup.PopupEntity(Loc.GetString("revenant-soul-begin-harvest", ("target", target)),
            target, Filter.Pvs(target), PopupType.Large);

        CanUseAbility(uid, revenant, 0, revenant.HarvestDuration, revenant.HarvestDuration);
        _doAfter.DoAfter(doAfter);
    }

    private void OnHarvestComplete(EntityUid uid, RevenantComponent component, HarvestDoAfterComplete args)
    {
        if (TryComp<AppearanceComponent>(uid, out var app))
            app.SetData(RevenantVisuals.Harvesting, false);

        if (!TryComp<EssenceComponent>(args.Target, out var essence))
            return;

        _popup.PopupEntity(Loc.GetString("revenant-soul-finish-harvest", ("target", args.Target)),
            args.Target, Filter.Pvs(args.Target), PopupType.LargeCaution);

        essence.Harvested = true;
        ChangeEssenceAmount(uid, essence.EssenceAmount, component);

        if (_mobState.IsAlive(args.Target) && _random.Prob(component.PerfectSoulChance))
        {
            _popup.PopupEntity(Loc.GetString("revenant-max-essence-increased"), uid, Filter.Entities(uid));
            component.MaxEssence = Math.Min(component.MaxEssence + component.MaxEssenceUpgradeAmount, component.EssenceCap);
        }

        if (TryComp<MobStateComponent>(args.Target, out var mobstate))
        {
            var damage = _mobState.GetEarliestDeadState(mobstate, 0)?.threshold;
            if (damage != null)
            {
                DamageSpecifier dspec = new();
                dspec.DamageDict.Add("Cellular", damage.Value);
                _damage.TryChangeDamage(args.Target, dspec, true);
            }
        }
    }

    private void OnHarvestCancelled(EntityUid uid, RevenantComponent component, HarvestDoAfterCancelled args)
    {
        if (TryComp<AppearanceComponent>(uid, out var app))
            app.SetData(RevenantVisuals.Harvesting, false);
    }
}

public sealed class SoulSearchDoAfterComplete : EntityEventArgs
{
    public readonly EntityUid Target;

    public SoulSearchDoAfterComplete(EntityUid target)
    {
        Target = target;
    }
}

public sealed class HarvestDoAfterComplete : EntityEventArgs
{
    public readonly EntityUid Target;

    public HarvestDoAfterComplete(EntityUid target)
    {
        Target = target;
    }
}

public sealed class HarvestDoAfterCancelled : EntityEventArgs { }

