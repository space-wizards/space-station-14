using System.Linq;
using Content.Shared.Actions;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using Content.Shared.Mobs;
using Content.Shared.Movement.Systems;
using Content.Shared.Nutrition;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Projectiles;
using Content.Shared.Prying.Components;
using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Prototypes;

namespace Content.Shared.Creatures.SpaceLeech;

/// <summary>
/// Handles the space leech's blood economy and upgrade effects.
/// Lives in shared so predicted events (movement speed, melee damage, ingestion)
/// resolve identically on client and server; all state changes are server-authoritative
/// through the networked <see cref="SpaceLeechComponent"/>.
/// </summary>
public sealed class SpaceLeechSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly SharedStealthSystem _stealth = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedBloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private static readonly ProtoId<SpaceLeechUpgradePrototype> PredatorUpgrade = "SpaceLeechUpgradePredator";
    private static readonly ProtoId<SpaceLeechUpgradePrototype> QuicknessUpgrade = "SpaceLeechUpgradeQuickness";
    private static readonly ProtoId<SpaceLeechUpgradePrototype> ShadowUpgrade = "SpaceLeechUpgradeShadow";
    private static readonly ProtoId<SpaceLeechUpgradePrototype> VenomUpgrade = "SpaceLeechUpgradeVenom";
    private static readonly ProtoId<SpaceLeechUpgradePrototype> RavenousUpgrade = "SpaceLeechUpgradeRavenous";
    private static readonly ProtoId<SpaceLeechUpgradePrototype> PryUpgrade = "SpaceLeechUpgradePry";
    private static readonly ProtoId<SpaceLeechUpgradePrototype> IronhideUpgrade = "SpaceLeechUpgradeIronhide";

    private static readonly ProtoId<ReagentPrototype> BloodReagent = "Blood";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpaceLeechComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SpaceLeechComponent, MobStateChangedEvent>(OnMobStateChanged);

        // Action → open upgrade menu
        SubscribeLocalEvent<SpaceLeechComponent, SpaceLeechUpgradeMenuActionEvent>(OnMenuAction);

        // Purchase messages from the upgrade menu (only ever received server-side).
        Subs.BuiEvents<SpaceLeechComponent>(SpaceLeechUiKey.UpgradeMenu, subs =>
        {
            subs.Event<SpaceLeechEvolveMessage>(OnEvolve);
        });

        // Runtime upgrade effects (event-based — no explicit refresh needed)
        SubscribeLocalEvent<SpaceLeechComponent, GetMeleeDamageEvent>(OnGetMeleeDamage);
        SubscribeLocalEvent<SpaceLeechComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
        SubscribeLocalEvent<SpaceLeechComponent, DamageModifyEvent>(OnDamageModify);

        // Sting hit — apply rank-scaled knockdown
        SubscribeLocalEvent<SpaceLeechStingProjectileComponent, ProjectileHitEvent>(OnStingHit);

        // Blood feeding
        SubscribeLocalEvent<SpaceLeechComponent, IngestingEvent>(OnIngesting);

        // Ravenous - speed up eating DoAfter; directed on PuddleComponent so it fires during RaiseLocalEvent(puddle, ref ev)
        // after IngestionSystem's OnEdible sets the base delay (broadcast=false means broadcast subs never fire).
        SubscribeLocalEvent<PuddleComponent, EdibleEvent>(OnEdible, after: [typeof(IngestionSystem)]);
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void OnMapInit(Entity<SpaceLeechComponent> ent, ref MapInitEvent args)
    {
        ApplyUpgrades(ent);
    }

    private void OnMobStateChanged(Entity<SpaceLeechComponent> ent, ref MobStateChangedEvent args)
    {
        // No hiding while downed or dead.
        _stealth.SetEnabled(ent, args.NewMobState == MobState.Alive);
    }

    // ── Upgrade menu ──────────────────────────────────────────────────────────

    private void OnMenuAction(Entity<SpaceLeechComponent> ent, ref SpaceLeechUpgradeMenuActionEvent args)
    {
        args.Handled = _ui.TryToggleUi(ent.Owner, SpaceLeechUiKey.UpgradeMenu, args.Performer);
    }

    private void OnEvolve(Entity<SpaceLeechComponent> ent, ref SpaceLeechEvolveMessage msg)
    {
        if (!_proto.TryIndex(msg.UpgradeId, out var upgrade))
            return;

        var rank = GetRank(ent.Comp, msg.UpgradeId);
        if (rank >= upgrade.MaxRank)
            return;

        var cost = upgrade.Costs[rank];
        if (ent.Comp.BloodPool < cost)
            return;

        ent.Comp.BloodPool -= cost;
        ent.Comp.UpgradeRanks[msg.UpgradeId] = rank + 1;
        Dirty(ent);

        ApplyUpgrades(ent);
    }

    // ── Upgrade application ───────────────────────────────────────────────────

    /// <summary>
    /// Pushes purchased ranks into the components that hold their config as plain fields.
    /// Only called on the server; the affected components network the changes themselves.
    /// </summary>
    private void ApplyUpgrades(Entity<SpaceLeechComponent> ent)
    {
        ApplyStealthUpgrade(ent);
        ApplyPryUpgrade(ent);
        ApplyStingUpgrade(ent);
        _movement.RefreshMovementSpeedModifiers(ent);
    }

    private void ApplyStealthUpgrade(Entity<SpaceLeechComponent> ent)
    {
        if (!TryComp<StealthOnMoveComponent>(ent, out var stealthMove))
            return;

        if (!_proto.TryIndex(ShadowUpgrade, out var upgrade))
            return;

        stealthMove.PassiveVisibilityRate = GetMagnitude(upgrade, GetRank(ent.Comp, ShadowUpgrade));
        Dirty(ent.Owner, stealthMove);
    }

    private void ApplyPryUpgrade(Entity<SpaceLeechComponent> ent)
    {
        if (!TryComp<PryingComponent>(ent, out var prying))
            return;

        if (!_proto.TryIndex(PryUpgrade, out var upgrade))
            return;

        // SpeedModifier is a divisor on pry time (time = base / modifier), so invert the time-fraction magnitude.
        var magnitude = GetMagnitude(upgrade, GetRank(ent.Comp, PryUpgrade));
        prying.SpeedModifier = magnitude > 0f ? 1f / magnitude : 1f;
        Dirty(ent.Owner, prying);
    }

    private void ApplyStingUpgrade(Entity<SpaceLeechComponent> ent)
    {
        var granted = GetRank(ent.Comp, VenomUpgrade) > 0;
        if (granted == (ent.Comp.StingActionEntity != null))
            return;

        if (granted)
        {
            _actions.AddAction(ent, ref ent.Comp.StingActionEntity, ent.Comp.StingAction);
        }
        else
        {
            _actions.RemoveAction(ent.Owner, ent.Comp.StingActionEntity);
            ent.Comp.StingActionEntity = null;
        }
    }

    // ── Event handlers for runtime upgrade effects ────────────────────────────

    private void OnGetMeleeDamage(Entity<SpaceLeechComponent> ent, ref GetMeleeDamageEvent args)
    {
        if (!TryGetPurchasedMagnitude(ent.Comp, PredatorUpgrade, out var coeff))
            return;

        args.Modifiers.Add(new DamageModifierSet
        {
            Coefficients = args.Damage.DamageDict.Keys.ToDictionary(k => k.Id, _ => coeff),
        });
    }

    private void OnRefreshSpeed(Entity<SpaceLeechComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (!TryGetPurchasedMagnitude(ent.Comp, QuicknessUpgrade, out var mult))
            return;

        args.ModifySpeed(mult, mult);
    }

    private void OnDamageModify(EntityUid uid, SpaceLeechComponent comp, DamageModifyEvent args)
    {
        if (!TryGetPurchasedMagnitude(comp, IronhideUpgrade, out var coeff))
            return;

        var modSet = new DamageModifierSet
        {
            Coefficients = args.Damage.DamageDict.Keys.ToDictionary(k => k.Id, _ => coeff),
        };
        args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, modSet);
    }

    private void OnStingHit(Entity<SpaceLeechStingProjectileComponent> ent, ref ProjectileHitEvent args)
    {
        if (args.Shooter is not { } shooter || !TryComp<SpaceLeechComponent>(shooter, out var leech))
            return;

        // The action is gated behind Venom rank 1, but stay safe against stray projectiles.
        if (!TryGetPurchasedMagnitude(leech, VenomUpgrade, out var seconds))
            return;

        _stun.TryKnockdown(args.Target, TimeSpan.FromSeconds(seconds), refresh: true, autoStand: true, drop: false, force: true);
    }

    // ── Blood feeding ─────────────────────────────────────────────────────────

    private void OnIngesting(Entity<SpaceLeechComponent> ent, ref IngestingEvent args)
    {
        // Divert blood reagents into the upgrade pool instead of metabolizing them.
        // Everything else stays in the split and metabolizes normally.
        var bloodGained = FixedPoint2.Zero;

        // Snapshot contents before removal to avoid mutating the collection mid-iteration.
        foreach (var quantity in args.Split.Contents.ToArray())
        {
            if (!IsBloodReagent(quantity.Reagent.Prototype))
                continue;

            bloodGained += quantity.Quantity;
            args.Split.RemoveReagent(quantity.Reagent, quantity.Quantity);
        }

        if (bloodGained == FixedPoint2.Zero)
            return;

        ent.Comp.BloodPool = FixedPoint2.Min(ent.Comp.BloodPool + bloodGained, ent.Comp.MaxBloodPool);
        ent.Comp.BloodConsumedTotal += bloodGained; // track all consumed, not just what fit in the pool
        Dirty(ent);

        // Restore a fraction of the leech's own bloodstream directly, bypassing metabolism.
        _bloodstream.TryModifyBloodLevel(ent.Owner, bloodGained * ent.Comp.BloodRestoreFraction);
    }

    private void OnEdible(Entity<PuddleComponent> ent, ref EdibleEvent args)
    {
        if (!TryComp<SpaceLeechComponent>(args.User, out var leech))
            return;

        if (!TryGetPurchasedMagnitude(leech, RavenousUpgrade, out var mult))
            return;

        args.Time *= mult;
    }

    /// <summary>
    /// A reagent counts as blood if it is the base blood reagent or inherits from it.
    /// The leech's own blood is a toxin outside that hierarchy, so it can't farm itself.
    /// </summary>
    private bool IsBloodReagent(string prototypeId)
    {
        if (prototypeId == BloodReagent.Id)
            return true;

        if (!_proto.HasIndex<ReagentPrototype>(prototypeId))
            return false;

        foreach (var parent in _proto.EnumerateParents<ReagentPrototype>(prototypeId))
        {
            if (parent.ID == BloodReagent.Id)
                return true;
        }

        return false;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    public static int GetRank(SpaceLeechComponent comp, ProtoId<SpaceLeechUpgradePrototype> upgrade)
    {
        return comp.UpgradeRanks.GetValueOrDefault(upgrade);
    }

    /// <summary>
    /// Gets the magnitude for the given rank, clamped to the configured list so a
    /// misconfigured prototype degrades gracefully instead of throwing.
    /// </summary>
    private static float GetMagnitude(SpaceLeechUpgradePrototype upgrade, int rank)
    {
        if (upgrade.Magnitudes.Count == 0)
            return 0f;

        return upgrade.Magnitudes[Math.Clamp(rank, 0, upgrade.Magnitudes.Count - 1)];
    }

    /// <summary>
    /// Looks up the magnitude of an upgrade's current rank.
    /// Returns false if the upgrade is unpurchased or the prototype is missing.
    /// </summary>
    private bool TryGetPurchasedMagnitude(SpaceLeechComponent comp, ProtoId<SpaceLeechUpgradePrototype> upgrade, out float magnitude)
    {
        magnitude = 0f;

        var rank = GetRank(comp, upgrade);
        if (rank == 0 || !_proto.TryIndex(upgrade, out var proto))
            return false;

        magnitude = GetMagnitude(proto, rank);
        return true;
    }
}
