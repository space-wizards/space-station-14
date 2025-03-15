using Content.Server.Actions;
using Content.Server.GameTicking;
using Content.Server.Revenant.Components;
using Content.Server.Store.Systems;
using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Eye;
using Content.Shared.FixedPoint;
using Content.Shared.Maps;
using Content.Shared.Mobs.Systems;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Revenant;
using Content.Shared.Revenant.Components;
using Content.Shared.Revenant.Systems;
using Content.Shared.StatusEffect;
using Content.Shared.Store.Components;
using Content.Shared.Stunnable;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Revenant.Systems;

public sealed partial class RevenantSystem : SharedRevenantSystem
{
    [Dependency] private readonly ActionsSystem _action = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly VisibilitySystem _visibility = default!;

    private readonly EntProtoId _revenantShopId = "ActionRevenantShop";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RevenantComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<RevenantComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RevenantComponent, RevenantShopActionEvent>(OnShop);
        SubscribeLocalEvent<RevenantComponent, DamageChangedEvent>(OnDamage);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(_ => MakeVisible(true));

        InitializeAbilities();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<RevenantComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime < comp.NextUpdateTime)
                continue;

            comp.NextUpdateTime = _timing.CurTime + comp.UpdateInterval;

            if (comp.Essence < comp.EssenceRegenCap)
                TryChangeEssenceAmount((uid, comp), comp.EssencePerUpdate, regenCap: true);
        }
    }

    private void OnStartup(Entity<RevenantComponent> ent, ref ComponentStartup args)
    {
        // Update the icon
        TryChangeEssenceAmount(ent.AsNullable(), 0);

        // Default the visuals
        Appearance.SetData(ent, RevenantVisuals.Corporeal, false);
        Appearance.SetData(ent, RevenantVisuals.Harvesting, false);
        Appearance.SetData(ent, RevenantVisuals.Stunned, false);

        if (_ticker.RunLevel == GameRunLevel.PostRound && TryComp<VisibilityComponent>(ent, out var visibility))
        {
            _visibility.AddLayer((ent, visibility), (int)VisibilityFlags.Ghost, false);
            _visibility.RemoveLayer((ent, visibility), (int)VisibilityFlags.Normal, false);
            _visibility.RefreshVisibility(ent, visibility);
        }

        // Ghost vision
        if (TryComp(ent, out EyeComponent? eye))
            _eye.SetVisibilityMask(ent, eye.VisibilityMask | (int)VisibilityFlags.Ghost, eye);
    }

    private void OnMapInit(Entity<RevenantComponent> ent, ref MapInitEvent args)
    {
        _action.AddAction(ent, ref ent.Comp.Action, _revenantShopId);
    }

    private void OnDamage(Entity<RevenantComponent> ent, ref DamageChangedEvent args)
    {
        if (!HasComp<CorporealComponent>(ent) || args.DamageDelta == null)
            return;

        var essenceDamage = args.DamageDelta.GetTotal().Float() * ent.Comp.DamageToEssenceCoefficient * -1;
        TryChangeEssenceAmount(ent.AsNullable(), essenceDamage);
    }

    /// <summary>
    ///     Attempts to change the essence amount for a <see cref="RevenantComponent" />.
    /// </summary>
    /// <param name="ent">The entity to change the essence amount for.</param>
    /// <param name="amount">The amount of essence.</param>
    /// <param name="allowDeath">Whether we should kill the entity if essence goes below zero.</param>
    /// <param name="regenCap">Whether the essence amount should be capped by the maximum set in the component.</param>
    /// <returns>True if successful, false otherwise.</returns>
    [PublicAPI]
    public bool TryChangeEssenceAmount(Entity<RevenantComponent?> ent,
        FixedPoint2 amount,
        bool allowDeath = true,
        bool regenCap = false)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        // Return false if we can't kill the entity but essence went below zero
        if (!allowDeath && ent.Comp.Essence + amount <= 0)
            return false;

        // Add it to the component
        ent.Comp.Essence += amount;
        Dirty(ent);

        // Check if we should cap it
        if (regenCap)
            FixedPoint2.Min(ent.Comp.Essence, ent.Comp.EssenceRegenCap);

        // Try to update the store UI
        if (TryComp<StoreComponent>(ent, out var store))
            _store.UpdateUserInterface(ent, ent, store);

        // Update the essence alert
        _alerts.ShowAlert(ent, ent.Comp.EssenceAlert);

        // Return early if essence is above zero
        if (ent.Comp.Essence > 0)
            return true;

        // Kill the entity otherwise
        Spawn(ent.Comp.SpawnOnDeathPrototype, Transform(ent).Coordinates);
        QueueDel(ent);
        return true;
    }

    private bool TryUseAbility(Entity<RevenantComponent> ent, RevenantActionComponent action)
    {
        if (ent.Comp.Essence <= action.Cost)
        {
            _popup.PopupEntity(Loc.GetString("revenant-not-enough-essence"), ent, ent);
            return false;
        }

        var tileref = Transform(ent).Coordinates.GetTileRef();
        if (tileref != null)
        {
            if (_physics.GetEntitiesIntersectingBody(ent, (int)CollisionGroup.Impassable).Count > 0)
            {
                _popup.PopupEntity(Loc.GetString("revenant-in-solid"), ent, ent);
                return false;
            }
        }

        TryChangeEssenceAmount(ent.AsNullable(), -action.Cost, false);

        _statusEffects.TryAddStatusEffect<CorporealComponent>(ent, "Corporeal", action.CorporealTime, false);
        _stun.TryStun(ent, action.StunTime, false);

        return true;
    }

    private void OnShop(Entity<RevenantComponent> ent, ref RevenantShopActionEvent args)
    {
        if (!TryComp<StoreComponent>(ent, out var store))
            return;
        _store.ToggleUi(ent, ent, store);
    }

    public void MakeVisible(bool visible)
    {
        var query = EntityQueryEnumerator<RevenantComponent, VisibilityComponent>();
        while (query.MoveNext(out var uid, out _, out var vis))
        {
            if (visible)
            {
                _visibility.AddLayer((uid, vis), (int)VisibilityFlags.Normal, false);
                _visibility.RemoveLayer((uid, vis), (int)VisibilityFlags.Ghost, false);
            }
            else
            {
                _visibility.AddLayer((uid, vis), (int)VisibilityFlags.Ghost, false);
                _visibility.RemoveLayer((uid, vis), (int)VisibilityFlags.Normal, false);
            }

            _visibility.RefreshVisibility(uid, vis);
        }
    }
}
