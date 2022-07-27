using Content.Server.Actions;
using Content.Server.Disease;
using Content.Shared.Popups;
using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.Interaction;
using Content.Shared.MobState.Components;
using Content.Server.DoAfter;
using Content.Shared.Stunnable;
using Content.Shared.Revenant;
using Robust.Server.GameObjects;
using Robust.Shared.Random;
using Content.Shared.StatusEffect;
using Content.Server.MobState;
using Content.Server.Visible;
using Content.Shared.Examine;
using Robust.Shared.Prototypes;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Tag;
using Content.Server.Polymorph.Systems;
using Robust.Shared.Player;

namespace Content.Server.Revenant.EntitySystems;

public sealed partial class RevenantSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly ActionsSystem _action = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly PolymorphableSystem _polymorphable = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly SharedInteractionSystem _interact = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly VisibilitySystem _visibility = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RevenantComponent, ComponentStartup>(OnInit);

        SubscribeLocalEvent<RevenantComponent, DamageChangedEvent>(OnDamage);
        SubscribeLocalEvent<RevenantComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<RevenantComponent, StatusEffectAddedEvent>(OnStatusAdded);
        SubscribeLocalEvent<RevenantComponent, StatusEffectEndedEvent>(OnStatusEnded);

        InitializeAbilities();
        InitializeShop();
    }

    private void OnInit(EntityUid uid, RevenantComponent component, ComponentStartup args)
    {
        //update the icon
        ChangeEssenceAmount(uid, 0, component);

        //default the visuals
        if (TryComp<AppearanceComponent>(uid, out var app))
        {
            app.SetData(RevenantVisuals.Corporeal, false);
            app.SetData(RevenantVisuals.Harvesting, false);
            app.SetData(RevenantVisuals.Stunned, false);
        }

        //give ghost vis flags
        var visibility = EntityManager.EnsureComponent<VisibilityComponent>(component.Owner);

        _visibility.AddLayer(visibility, (int) VisibilityFlags.Ghost, false);
        _visibility.RemoveLayer(visibility, (int) VisibilityFlags.Normal, false);
        _visibility.RefreshVisibility(visibility);

        //ghost vision
        if (TryComp(component.Owner, out EyeComponent? eye))
            eye.VisibilityMask |= (uint) (VisibilityFlags.Ghost);

        //get all the abilities
        foreach (var listing in _proto.EnumeratePrototypes<RevenantStoreListingPrototype>())
            component.Listings.Add(listing);

        //TODO: kill
        var action = new InstantAction(_proto.Index<InstantActionPrototype>("RevenantShop"));
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
        if (!HasComp<CorporealComponent>(uid) || args.DamageDelta == null)
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

        _alerts.ShowAlert(uid, AlertType.Essence, (short) Math.Round(component.Essence / 10f));

        if (component.Essence <= 0)
        {
            component.Essence = component.MaxEssence;
            _polymorphable.PolymorphEntity(uid, "Ectoplasm");
        }

        UpdateUserInterface(component);
        return true;
    }

    private bool CanUseAbility(EntityUid uid, RevenantComponent component, float abilityCost, Vector2 debuffs)
    {
        if (!ChangeEssenceAmount(uid, abilityCost, component, false))
        {
            _popup.PopupEntity(Loc.GetString("revenant-not-enough-essence"), uid, Filter.Entities(uid));
            return false;
        }

        _statusEffects.TryAddStatusEffect<CorporealComponent>(uid, "Corporeal", TimeSpan.FromSeconds(debuffs.Y), false);
        _stun.TryStun(uid, TimeSpan.FromSeconds(debuffs.X), false);

        return true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var rev in EntityQuery<RevenantComponent>())
        {
            rev.Accumulator += frameTime;

            if (rev.Accumulator <= rev.TickDuration)
                continue;
            rev.Accumulator -= rev.TickDuration;

            if (rev.Essence < rev.MaxEssence)
            {
                ChangeEssenceAmount(rev.Owner, rev.EssencePerTick, rev);
            }
        }
    }
}
