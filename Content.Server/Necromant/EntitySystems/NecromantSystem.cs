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
using Content.Shared.Maps;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Necromant;
using Content.Shared.Necromant.Components;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Necromant.EntitySystems;

public sealed partial class NecromantSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ActionsSystem _action = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly StoreSystem _store = default!;


    [ValidatePrototypeId<EntityPrototype>]
    private const string NecromantShopId = "ActionNecromantShop";

    public override void Initialize()
    {
        base.Initialize();


        SubscribeLocalEvent<NecromantComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<NecromantComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<NecromantComponent, DamageChangedEvent>(OnDamage);
        SubscribeLocalEvent<NecromantComponent, NecromantShopActionEvent>(OnShop);
        SubscribeLocalEvent<NecromantComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<NecromantComponent, StatusEffectAddedEvent>(OnStatusAdded);
        SubscribeLocalEvent<NecromantComponent, StatusEffectEndedEvent>(OnStatusEnded);

        InitializeAbilities();
    }



    private void OnStartup(EntityUid uid, NecromantComponent component, ComponentStartup args)
    {
        //update the icon
        ChangeEssenceAmount(uid, 0, component);

        //default the visuals
        _appearance.SetData(uid, NecromantVisuals.Harvesting, false);
        _appearance.SetData(uid, NecromantVisuals.Stunned, false);

    }

    private void OnMapInit(EntityUid uid, NecromantComponent component, MapInitEvent args)
    {
        _action.AddAction(uid, NecromantShopId);
    }

    private void OnStatusAdded(EntityUid uid, NecromantComponent component, StatusEffectAddedEvent args)
    {
        if (args.Key == "Stun")
            _appearance.SetData(uid, NecromantVisuals.Stunned, true);
    }

    private void OnStatusEnded(EntityUid uid, NecromantComponent component, StatusEffectEndedEvent args)
    {
        if (args.Key == "Stun")
            _appearance.SetData(uid, NecromantVisuals.Stunned, false);
    }

    private void OnExamine(EntityUid uid, NecromantComponent component, ExaminedEvent args)
    {
        if (args.Examiner == args.Examined)
        {
            args.PushMarkup(Loc.GetString("revenant-essence-amount",
                ("current", component.Essence.Int()), ("max", component.EssenceRegenCap.Int())));
        }
    }

    private void OnDamage(EntityUid uid, NecromantComponent component, DamageChangedEvent args)
    {
        if (args.DamageDelta == null)
            return;

        var essenceDamage = args.DamageDelta.Total.Float() * component.DamageToEssenceCoefficient * -1;
        ChangeEssenceAmount(uid, essenceDamage, component);
    }


    public bool ChangeEssenceAmount(EntityUid uid, FixedPoint2 amount, NecromantComponent? component = null, bool allowDeath = true, bool regenCap = false)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!allowDeath && component.Essence + amount <= 0)
            return false;

        component.Essence += amount;

        if (regenCap)
            FixedPoint2.Min(component.Essence, component.EssenceRegenCap);

        if (TryComp<StoreComponent>(uid, out var store))
            _store.UpdateUserInterface(uid, uid, store);

        _alerts.ShowAlert(uid, AlertType.Essence, (short) Math.Clamp(Math.Round(component.Essence.Float() / 10f), 0, 16));

        if (component.Essence <= 0)
        {
            QueueDel(uid);
        }
        return true;
    }

    private bool TryUseAbility(EntityUid uid, NecromantComponent component, FixedPoint2 abilityCost, Vector2 debuffs)
    {
        if (component.Essence <= abilityCost)
        {
            _popup.PopupEntity(Loc.GetString("revenant-not-enough-essence"), uid, uid);
            return false;
        }

        ChangeEssenceAmount(uid, abilityCost, component, false);

        _stun.TryStun(uid, TimeSpan.FromSeconds(debuffs.X), false);

        return true;
    }

    private void OnShop(EntityUid uid, NecromantComponent component, NecromantShopActionEvent args)
    {
        if (!TryComp<StoreComponent>(uid, out var store))
            return;
        _store.ToggleUi(uid, uid, store);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var rev in EntityQuery<NecromantComponent>())
        {
            rev.Accumulator += frameTime;

            if (rev.Accumulator <= 1)
                continue;
            rev.Accumulator -= 1;

            if (rev.Essence < rev.EssenceRegenCap)
            {
                ChangeEssenceAmount(rev.Owner, rev.EssencePerSecond, rev, regenCap: true);
            }
        }
    }



}
