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
using Content.Shared.Examine;
using Robust.Shared.Prototypes;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Tag;
using Content.Server.Storage.EntitySystems;
using Content.Server.Storage.Components;

namespace Content.Server.Revenant;

public sealed class RevenantSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly VisibilitySystem _visibility = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly SharedInteractionSystem _interact = default!;
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly ActionsSystem _action = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly DiseaseSystem _disease = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RevenantComponent, ComponentStartup>(OnInit);

        SubscribeLocalEvent<RevenantComponent, DamageChangedEvent>(OnDamage);
        SubscribeLocalEvent<RevenantComponent, SpeakAttemptEvent>(OnSpeakAttempt);
        SubscribeLocalEvent<RevenantComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<RevenantComponent, StatusEffectAddedEvent>(OnStatusAdded);
        SubscribeLocalEvent<RevenantComponent, StatusEffectEndedEvent>(OnStatusEnded);

        SubscribeLocalEvent<RevenantComponent, InteractNoHandEvent>(OnInteract);
        SubscribeLocalEvent<RevenantComponent, SoulSearchDoAfterComplete>(OnSoulSearchComplete);
        SubscribeLocalEvent<RevenantComponent, HarvestDoAfterComplete>(OnHarvestComplete);
        SubscribeLocalEvent<RevenantComponent, HarvestDoAfterCancelled>(OnHarvestCancelled);
        SubscribeLocalEvent<RevenantComponent, RevenantDefileActionEvent>(OnDefileAction);
    }

    private void OnInit(EntityUid uid, RevenantComponent component, ComponentStartup args)
    {
        //update the icon.
        ChangeEssenceAmount(uid, 0, component);

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

        var action = new InstantAction(_proto.Index<InstantActionPrototype>("RevenantDefile"));
        _action.AddAction(uid, action, null);
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

    private void OnExamine(EntityUid uid, RevenantComponent component, ExaminedEvent args)
    {
        if (args.Examiner == args.Examined)
        {
            args.PushMarkup(Loc.GetString("revenant-essence-amount",
                ("current", Math.Round(component.Essence)), ("max", Math.Round(component.MaxEssence))));
        }
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

    private void OnDamage(EntityUid uid, RevenantComponent component, DamageChangedEvent args)
    {
        if (args.DamageDelta == null)
            return;

        var essenceDamage = args.DamageDelta.Total.Float() * component.DamageToEssenceCoefficient * -1;
        ChangeEssenceAmount(uid, essenceDamage, component);
    }

    public bool ChangeEssenceAmount(EntityUid uid, float amount, RevenantComponent? component = null, bool allowDeath = true)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!allowDeath && component.Essence + amount <= 0)
            return false;

        component.Essence = Math.Min(component.Essence + amount, component.MaxEssence);

        //change this into morph into ectoplasm
        if (component.Essence <= 0)
            EntityManager.QueueDeleteEntity(uid);

        _alerts.ShowAlert(uid, AlertType.Essence, (short) Math.Round(component.Essence / 10f));

        return true;
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

        _statusEffects.TryAddStatusEffect<CorporealComponent>(uid, "Corporeal", TimeSpan.FromSeconds(revenant.HarvestDuration), false);
        _stun.TryStun(uid, TimeSpan.FromSeconds(revenant.HarvestDuration), false);
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

        if (_mobState.IsAlive(args.Target))
        {
            _popup.PopupEntity(Loc.GetString("revenant-max-essence-increased"), uid, Filter.Entities(uid));
            component.MaxEssence += 5;
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

    private void OnDefileAction(EntityUid uid, RevenantComponent component, RevenantDefileActionEvent args)
    {
        if (args.Handled)
            return;

        if (!ChangeEssenceAmount(uid, component.DefileUseCost, component, false))
        {
            _popup.PopupEntity(Loc.GetString("revenant-not-enough-essence"), uid, Filter.Entities(uid));
            return;
        }

        args.Handled = true;

        _statusEffects.TryAddStatusEffect<CorporealComponent>(uid, "Corporeal", TimeSpan.FromSeconds(component.DefileCorporealDuration), false);
        _stun.TryStun(uid, TimeSpan.FromSeconds(component.DefileStunDuration), false);

        var lookup = _lookup.GetEntitiesInRange(uid, component.DefileRadius, LookupFlags.Approximate | LookupFlags.Anchored);
        var tags = GetEntityQuery<TagComponent>();
        var storage = GetEntityQuery<EntityStorageComponent>();

        foreach (var ent in lookup)
        {
            //break windows
            if (tags.HasComponent(ent))
            {
                if (_tag.HasAnyTag(ent, "Window"))
                {
                    var dspec = new DamageSpecifier();
                    dspec.DamageDict.Add("Structural", 15);
                    _damage.TryChangeDamage(ent, dspec);
                }
            }

            if (storage.HasComponent(ent))
            {
                if (_random.Prob(0.8f)) //arbitrary number
                    _entityStorage
            }
        }
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

