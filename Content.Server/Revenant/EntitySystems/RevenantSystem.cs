using System.Numerics;
using Content.Server.Actions;
using Content.Server.GameTicking;
using Content.Server.Store.Components;
using Content.Server.Store.Systems;
using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Eye;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Mobs.Systems;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Revenant;
using Content.Shared.Revenant.Components;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Revenant.EntitySystems;

public sealed partial class RevenantSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ActionsSystem _action = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly SharedInteractionSystem _interact = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly VisibilitySystem _visibility = default!;

    [ValidatePrototypeId<EntityPrototype>]
    private const string RevenantShopId = "ActionRevenantShop";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RevenantComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<RevenantComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<RevenantComponent, RevenantShopActionEvent>(OnShop);
        SubscribeLocalEvent<RevenantComponent, DamageChangedEvent>(OnDamage);
        SubscribeLocalEvent<RevenantComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<RevenantComponent, StatusEffectAddedEvent>(OnStatusAdded);
        SubscribeLocalEvent<RevenantComponent, StatusEffectEndedEvent>(OnStatusEnded);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(_ => MakeVisible(true));

        InitializeAbilities();
    }

    private void OnStartup(EntityUid uid, RevenantComponent component, ComponentStartup args)
    {
        //update the icon
        ChangeEssenceAmount(uid, 0, component);

        //default the visuals
        _appearance.SetData(uid, RevenantVisuals.Corporeal, false);
        _appearance.SetData(uid, RevenantVisuals.Harvesting, false);
        _appearance.SetData(uid, RevenantVisuals.Stunned, false);

        if (_ticker.RunLevel == GameRunLevel.PostRound && TryComp<VisibilityComponent>(uid, out var visibility))
        {
            _visibility.AddLayer(uid, visibility, (int) VisibilityFlags.Ghost, false);
            _visibility.RemoveLayer(uid, visibility, (int) VisibilityFlags.Normal, false);
            _visibility.RefreshVisibility(uid, visibility);
        }

        //ghost vision
        if (TryComp(uid, out EyeComponent? eye))
        {
            _eye.SetVisibilityMask(uid, eye.VisibilityMask | (int) (VisibilityFlags.Ghost), eye);
        }
    }

    private void OnMapInit(EntityUid uid, RevenantComponent component, MapInitEvent args)
    {
        _action.AddAction(uid, ref component.Action, RevenantShopId);
    }

    private void OnStatusAdded(EntityUid uid, RevenantComponent component, StatusEffectAddedEvent args)
    {
        if (args.Key == "Stun")
            _appearance.SetData(uid, RevenantVisuals.Stunned, true);
    }

    private void OnStatusEnded(EntityUid uid, RevenantComponent component, StatusEffectEndedEvent args)
    {
        if (args.Key == "Stun")
            _appearance.SetData(uid, RevenantVisuals.Stunned, false);
    }

    private void OnExamine(EntityUid uid, RevenantComponent component, ExaminedEvent args)
    {
        if (args.Examiner == args.Examined)
        {
            args.PushMarkup(Loc.GetString("revenant-essence-amount",
                ("current", component.Essence.Int()), ("max", component.EssenceRegenCap.Int())));
        }
    }

    private void OnDamage(EntityUid uid, RevenantComponent component, DamageChangedEvent args)
    {
        if (!HasComp<CorporealComponent>(uid) || args.DamageDelta == null)
            return;

        var essenceDamage = args.DamageDelta.GetTotal().Float() * component.DamageToEssenceCoefficient * -1;
        ChangeEssenceAmount(uid, essenceDamage, component);
    }

    public bool ChangeEssenceAmount(EntityUid uid, FixedPoint2 amount, RevenantComponent? component = null, bool allowDeath = true, bool regenCap = false)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!allowDeath && component.Essence + amount <= 0)
            return false;

        component.Essence += amount;
        Dirty(uid, component);

        if (regenCap)
            FixedPoint2.Min(component.Essence, component.EssenceRegenCap);

        if (TryComp<StoreComponent>(uid, out var store))
            _store.UpdateUserInterface(uid, uid, store);

        _alerts.ShowAlert(uid, AlertType.Essence);

        if (component.Essence <= 0)
        {
            Spawn(component.SpawnOnDeathPrototype, Transform(uid).Coordinates);
            QueueDel(uid);
        }
        return true;
    }

    private bool TryUseAbility(EntityUid uid, RevenantComponent component, FixedPoint2 abilityCost, Vector2 debuffs)
    {
        if (component.Essence <= abilityCost)
        {
            _popup.PopupEntity(Loc.GetString("revenant-not-enough-essence"), uid, uid);
            return false;
        }

        var tileref = Transform(uid).Coordinates.GetTileRef();
        if (tileref != null)
        {
            if(_physics.GetEntitiesIntersectingBody(uid, (int) CollisionGroup.Impassable).Count > 0)
            {
                _popup.PopupEntity(Loc.GetString("revenant-in-solid"), uid, uid);
                return false;
            }
        }

        ChangeEssenceAmount(uid, abilityCost, component, false);

        _statusEffects.TryAddStatusEffect<CorporealComponent>(uid, "Corporeal", TimeSpan.FromSeconds(debuffs.Y), false);
        _stun.TryStun(uid, TimeSpan.FromSeconds(debuffs.X), false);

        return true;
    }

    private void OnShop(EntityUid uid, RevenantComponent component, RevenantShopActionEvent args)
    {
        if (!TryComp<StoreComponent>(uid, out var store))
            return;
        _store.ToggleUi(uid, uid, store);
    }

    public void MakeVisible(bool visible)
    {
        var query = EntityQueryEnumerator<RevenantComponent, VisibilityComponent>();
        while (query.MoveNext(out var uid, out _, out var vis))
        {
            if (visible)
            {
                _visibility.AddLayer(uid, vis, (int) VisibilityFlags.Normal, false);
                _visibility.RemoveLayer(uid, vis, (int) VisibilityFlags.Ghost, false);
            }
            else
            {
                _visibility.AddLayer(uid, vis, (int) VisibilityFlags.Ghost, false);
                _visibility.RemoveLayer(uid, vis, (int) VisibilityFlags.Normal, false);
            }
            _visibility.RefreshVisibility(uid, vis);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<RevenantComponent>();
        while (query.MoveNext(out var uid, out var rev))
        {
            rev.Accumulator += frameTime;

            if (rev.Accumulator <= 1)
                continue;
            rev.Accumulator -= 1;

            if (rev.Essence < rev.EssenceRegenCap)
            {
                ChangeEssenceAmount(uid, rev.EssencePerSecond, rev, regenCap: true);
            }
        }
    }
}
