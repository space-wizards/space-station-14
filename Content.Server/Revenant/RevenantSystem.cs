using Content.Server.Actions;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Disease;
using Content.Server.Mind.Components;
using Content.Shared.Popups;
using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.Interaction;
using Content.Shared.MobState;
using Content.Shared.MobState.Components;
using Content.Shared.Physics;
using Content.Server.DoAfter;
using Content.Shared.Stunnable;
using Content.Shared.Revenant;
using Robust.Server.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Random;
using System.Linq;
using System.Threading;
using Content.Shared.StatusEffect;
using Robust.Shared.Player;
using Content.Server.MobState;
using Content.Shared.Speech;
using Content.Server.Visible;

namespace Content.Server.Revenant;

public sealed class RevenantSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly VisibilitySystem _visibility = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly SharedInteractionSystem _interact = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly ActionsSystem _action = default!;
    [Dependency] private readonly DiseaseSystem _disease = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RevenantComponent, ComponentStartup>(OnInit);

        SubscribeLocalEvent<RevenantComponent, DamageChangedEvent>(OnDamage);
        SubscribeLocalEvent<RevenantComponent, SpeakAttemptEvent>(OnSpeakAttempt);
        SubscribeLocalEvent<RevenantComponent, StatusEffectAddedEvent>(OnStatusAdded);
        SubscribeLocalEvent<RevenantComponent, StatusEffectEndedEvent>(OnStatusEnded);

        SubscribeLocalEvent<RevenantComponent, InteractNoHandEvent>(OnInteract);
        SubscribeLocalEvent<RevenantComponent, SoulSearchDoAfterComplete>(OnSoulSearchComplete);
        SubscribeLocalEvent<RevenantComponent, HarvestDoAfterComplete>(OnHarvestComplete);
        SubscribeLocalEvent<RevenantComponent, HarvestDoAfterCancelled>(OnHarvestCancelled);
    }

    private void OnInit(EntityUid uid, RevenantComponent component, ComponentStartup args)
    {
        if (TryComp<AppearanceComponent>(uid, out var app))
        {
            app.SetData(RevenantVisuals.Corporeal, false);
            app.SetData(RevenantVisuals.Harvesting, false);
            app.SetData(RevenantVisuals.Stunned, false);
        }

        var visibility = EntityManager.EnsureComponent<VisibilityComponent>(component.Owner);

        _visibility.AddLayer(visibility, (int) VisibilityFlags.Ghost, false);
        _visibility.RemoveLayer(visibility, (int) VisibilityFlags.Normal, false);
        _visibility.RefreshVisibility(visibility);

        if (TryComp(component.Owner, out EyeComponent? eye))
        {
            eye.VisibilityMask |= (uint) (VisibilityFlags.Ghost);
        }
    }

    private void OnStatusAdded(EntityUid uid, RevenantComponent component, StatusEffectAddedEvent args)
    {
        if (!TryComp<AppearanceComponent>(uid, out var app))
            return;

        if (args.Key == "Stun")
            app.SetData(RevenantVisuals.Stunned, true);
    }

    private void OnStatusEnded(EntityUid uid, RevenantComponent component, StatusEffectEndedEvent args)
    {
        if (!TryComp<AppearanceComponent>(uid, out var app))
            return;

        if (args.Key == "Stun")
            app.SetData(RevenantVisuals.Stunned, false);
    }

    private void OnSpeakAttempt(EntityUid uid, RevenantComponent component, SpeakAttemptEvent args)
    {
        if (!HasComp<CorporealComponent>(uid))
            args.Cancel();
    }

    private void OnInteract(EntityUid uid, RevenantComponent component, InteractNoHandEvent args)
    {
        var target = args.Target;
        if (target == args.User)
            return;

        if (!HasComp<MobStateComponent>(target) || HasComp<RevenantComponent>(target))
            return;

        if (!_interact.InRangeUnobstructed(uid, target))
            return;

        if (!TryComp<EssenceComponent>(target, out var essence) || !essence.SearchComplete)
        {
            if (essence == null)
                essence = EnsureComp<EssenceComponent>(target);
            BeginSoulSearchDoAfter(uid, target, component, essence);
        }
        else
        {
            BeginHarvestDoAfter(uid, target, component, essence);
        }
    }

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

        _stun.TryStun(uid, TimeSpan.FromSeconds(revenant.HarvestDuration), false);
        _statusEffects.TryAddStatusEffect<CorporealComponent>(uid, "Corporeal", TimeSpan.FromSeconds(5), false);
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

        if (_mobState.IsAlive(args.Target))
            component.MaxEssence += 5;

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

        essence.Harvested = true;
        component.Essence = Math.Min(component.Essence + essence.EssenceAmount, component.MaxEssence);
    }

    private void OnHarvestCancelled(EntityUid uid, RevenantComponent component, HarvestDoAfterCancelled args)
    {
        if (TryComp<AppearanceComponent>(uid, out var app))
            app.SetData(RevenantVisuals.Harvesting, false);
    }

    private void OnDamage(EntityUid uid, RevenantComponent component, DamageChangedEvent args)
    {
        if (args.DamageDelta == null)
            return;

        var essenceDamage = args.DamageDelta.Total.Float() * component.DamageToEssenceCoefficient;
        component.Essence = Math.Min(component.Essence - essenceDamage, component.MaxEssence);

        if (component.Essence <= 0)
            EntityManager.QueueDeleteEntity(uid);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var rev in EntityQuery<RevenantComponent>())
        {
            rev.Accumulator += frameTime;

            if (rev.Accumulator <= 10f)
                continue;
            rev.Accumulator -= 10f;

            if (rev.Essence < rev.MaxEssence)
            {
                rev.Essence += rev.EssencePerSecond;
                rev.Essence = Math.Min(rev.Essence, rev.MaxEssence); //you're not squeaking out any extra essence
            }
        }
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
