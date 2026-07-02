using System.Linq;
using Content.Server.Stunnable;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Creatures.SpaceLeech;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Movement.Systems;
using Content.Shared.Fluids.Components;
using Content.Shared.Nutrition;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Projectiles;
using Content.Shared.Prying.Components;
using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;
using Content.Shared.Weapons.Melee.Events;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.Creatures.SpaceLeech;

public sealed class SpaceLeechSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedStealthSystem _stealth = default!;
    [Dependency] private readonly StunSystem _stun = default!;
    [Dependency] private readonly SharedBloodstreamSystem _bloodstream = default!;

    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpaceLeechComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SpaceLeechComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<SpaceLeechComponent, MindAddedMessage>(OnMindAdded);

        // Action → open upgrade menu
        SubscribeLocalEvent<SpaceLeechComponent, SpaceLeechUpgradeMenuActionEvent>(OnMenuAction);

        // BUI lifecycle
        Subs.BuiEvents<SpaceLeechComponent>(SpaceLeechUiKey.UpgradeMenu, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnMenuOpened);
            subs.Event<SpaceLeechEvolveMessage>(OnEvolve);
        });

        // Runtime upgrade effects (event-based — no explicit refresh needed)
        SubscribeLocalEvent<SpaceLeechComponent, GetMeleeDamageEvent>(OnGetMeleeDamage);
        SubscribeLocalEvent<SpaceLeechComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
        SubscribeLocalEvent<SpaceLeechComponent, DamageModifyEvent>(OnDamageModify);

        // Sting hit — apply rank-scaled knockdown
        SubscribeLocalEvent<SpaceLeechStingProjectileComponent, ProjectileHitEvent>(OnStingHit);

        // Blood feeding
        SubscribeLocalEvent<SpaceLeechComponent, IngestingEvent>(OnSpaceLeechIngesting);

        // Ravenous - speed up eating DoAfter; directed on PuddleComponent so it fires during RaiseLocalEvent(puddle, ref ev)
        // after IngestionSystem's OnEdible sets the base delay (broadcast=false means broadcast subs never fire).
        SubscribeLocalEvent<PuddleComponent, EdibleEvent>(OnSpaceLeechEdible,
            after: [typeof(IngestionSystem)]);
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void OnMapInit(EntityUid uid, SpaceLeechComponent comp, MapInitEvent args)
    {
        ApplyUpgrades(uid, comp);
    }

    private void OnMobStateChanged(EntityUid uid, SpaceLeechComponent comp, MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
            _stealth.SetEnabled(uid, false);
    }

    private void OnMindAdded(EntityUid uid, SpaceLeechComponent comp, MindAddedMessage args)
    {
        _mind.TryAddObjective(args.Mind, args.Mind.Comp, "SpaceLeechSurviveObjective");
        _mind.TryAddObjective(args.Mind, args.Mind.Comp, "SpaceLeechBloodObjective");
        _mind.TryAddObjective(args.Mind, args.Mind.Comp, "SpaceLeechPetBloodObjective");
    }

    // ── BUI ───────────────────────────────────────────────────────────────────

    private void OnMenuAction(EntityUid uid, SpaceLeechComponent comp, SpaceLeechUpgradeMenuActionEvent args)
    {
        _uiSystem.TryToggleUi(uid, SpaceLeechUiKey.UpgradeMenu, args.Performer);
    }

    private void OnMenuOpened(EntityUid uid, SpaceLeechComponent comp, BoundUIOpenedEvent args)
    {
        ApplyUpgrades(uid, comp);
        UpdateBuiState(uid, comp);
    }

    private void OnEvolve(EntityUid uid, SpaceLeechComponent comp, SpaceLeechEvolveMessage msg)
    {
        if (!_proto.TryIndex<SpaceLeechUpgradePrototype>(msg.UpgradeId, out var upgrade))
            return;

        comp.UpgradeRanks.TryGetValue(msg.UpgradeId, out var currentRank);

        if (currentRank >= SpaceLeechUpgradePrototype.MaxRank)
            return;

        var cost = upgrade.Costs[currentRank];
        if (comp.BloodPool < cost)
            return;

        comp.BloodPool -= cost;
        comp.UpgradeRanks[msg.UpgradeId] = currentRank + 1;
        Dirty(uid, comp);

        ApplyUpgrades(uid, comp);
        UpdateBuiState(uid, comp);
    }

    public void UpdateBuiState(EntityUid uid, SpaceLeechComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        _uiSystem.SetUiState(uid, SpaceLeechUiKey.UpgradeMenu,
            new SpaceLeechUpgradeMenuBuiState(
                comp.BloodPool,
                comp.MaxBloodPool,
                comp.BloodConsumedTotal,
                new Dictionary<string, int>(comp.UpgradeRanks)));
    }

    // ── Upgrade application ───────────────────────────────────────────────────

    private void ApplyUpgrades(EntityUid uid, SpaceLeechComponent comp)
    {
        comp.UpgradeRanks.TryGetValue("SpaceLeechUpgradeShadow", out var shadowRank);
        ApplyStealthUpgrade(uid, shadowRank);

        comp.UpgradeRanks.TryGetValue("SpaceLeechUpgradePry", out var pryRank);
        ApplyPryUpgrade(uid, pryRank);

        _movement.RefreshMovementSpeedModifiers(uid);
    }

    private void ApplyStealthUpgrade(EntityUid uid, int rank)
    {
        if (!TryComp<StealthOnMoveComponent>(uid, out var stealthMove))
            return;

        if (!_proto.TryIndex<SpaceLeechUpgradePrototype>("SpaceLeechUpgradeShadow", out var upgrade))
            return;

        stealthMove.PassiveVisibilityRate = upgrade.Magnitudes[rank];
    }

    private void ApplyPryUpgrade(EntityUid uid, int rank)
    {
        if (!TryComp<PryingComponent>(uid, out var prying))
            return;

        if (!_proto.TryIndex<SpaceLeechUpgradePrototype>("SpaceLeechUpgradePry", out var upgrade))
            return;

        // SpeedModifier is a divisor on pry time (time = base / modifier), so invert the time-fraction magnitude.
        prying.SpeedModifier = rank == 0 ? 1.0f : 1.0f / upgrade.Magnitudes[rank];
    }

    // ── Event handlers for runtime upgrade effects ────────────────────────────

    private void OnGetMeleeDamage(EntityUid uid, SpaceLeechComponent comp, ref GetMeleeDamageEvent args)
    {
        comp.UpgradeRanks.TryGetValue("SpaceLeechUpgradePredator", out var rank);
        if (rank == 0)
            return;

        if (!_proto.TryIndex<SpaceLeechUpgradePrototype>("SpaceLeechUpgradePredator", out var upgrade))
            return;

        var coeff = upgrade.Magnitudes[rank];
        args.Modifiers.Add(new DamageModifierSet
        {
            Coefficients = args.Damage.DamageDict.Keys.ToDictionary(k => k.Id, _ => coeff),
        });
    }

    private void OnRefreshSpeed(EntityUid uid, SpaceLeechComponent comp, RefreshMovementSpeedModifiersEvent args)
    {
        comp.UpgradeRanks.TryGetValue("SpaceLeechUpgradeQuickness", out var rank);
        if (rank == 0)
            return;

        if (!_proto.TryIndex<SpaceLeechUpgradePrototype>("SpaceLeechUpgradeQuickness", out var upgrade))
            return;

        var mult = upgrade.Magnitudes[rank];
        args.ModifySpeed(mult, mult);
    }

    private void OnStingHit(EntityUid uid, SpaceLeechStingProjectileComponent comp, ref ProjectileHitEvent args)
    {
        if (args.Shooter is not { } shooter || !TryComp<SpaceLeechComponent>(shooter, out var leech))
            return;

        leech.UpgradeRanks.TryGetValue("SpaceLeechUpgradeVenom", out var rank);

        if (!_proto.TryIndex<SpaceLeechUpgradePrototype>("SpaceLeechUpgradeVenom", out var upgrade))
            return;

        var stunTime = TimeSpan.FromSeconds(upgrade.Magnitudes[rank]);
        _stun.TryKnockdown(args.Target, stunTime, refresh: true, autoStand: true, drop: false, force: true);
    }

    private void OnSpaceLeechEdible(EntityUid uid, PuddleComponent comp, ref EdibleEvent args)
    {
        if (!TryComp<SpaceLeechComponent>(args.User, out var leech))
            return;

        leech.UpgradeRanks.TryGetValue("SpaceLeechUpgradeRavenous", out var rank);
        if (rank == 0)
            return;

        if (!_proto.TryIndex<SpaceLeechUpgradePrototype>("SpaceLeechUpgradeRavenous", out var upgrade))
            return;

        args.Time = TimeSpan.FromSeconds(args.Time.TotalSeconds * upgrade.Magnitudes[rank]);
    }

    private void OnSpaceLeechIngesting(EntityUid uid, SpaceLeechComponent comp, ref IngestingEvent args)
    {
        // Measure blood reagents for the pool, then clear the entire split so nothing is metabolized.
        // Clearing everything prevents toxicity from non-standard blood types and any accompanying reagents.
        var bloodGained = 0f;

        // Snapshot contents before removal to avoid mutating the collection mid-iteration.
        var contents = args.Split.Contents.ToArray();
        foreach (var rq in contents)
        {
            if (IsBloodReagent(rq.Reagent.Prototype))
                bloodGained += (float) rq.Quantity;
            args.Split.RemoveReagent(rq.Reagent, rq.Quantity);
        }

        if (bloodGained == 0f)
            return;

        // Add to upgrade blood pool resource
        var absorbed = Math.Min(bloodGained, comp.MaxBloodPool - comp.BloodPool);
        comp.BloodPool += absorbed;
        comp.BloodConsumedTotal += bloodGained; // track all consumed, not just what fit in the pool
        Dirty(uid, comp);
        UpdateBuiState(uid, comp);

        // Restore a fraction of the space leech's own bloodstream directly — bypasses metabolism so FerrochromicAcid is never injected.
        _bloodstream.TryModifyBloodLevel(uid, FixedPoint2.New(bloodGained * 0.2f));
    }

    private bool IsBloodReagent(string prototypeId)
    {
        // Exclude the space leech's own blood type to prevent drinking spilled self-blood for infinite resources/healing.
        if (prototypeId == "FerrochromicAcid")
            return false;

        if (prototypeId == "Blood")
            return true;

        return _proto.TryIndex<ReagentPrototype>(prototypeId, out var proto)
               && proto.Parents != null
               && proto.Parents.Contains("Blood");
    }

    private void OnDamageModify(EntityUid uid, SpaceLeechComponent comp, DamageModifyEvent args)
    {
        comp.UpgradeRanks.TryGetValue("SpaceLeechUpgradeIronhide", out var rank);
        if (rank == 0)
            return;

        if (!_proto.TryIndex<SpaceLeechUpgradePrototype>("SpaceLeechUpgradeIronhide", out var upgrade))
            return;

        var coeff = upgrade.Magnitudes[rank];
        var modSet = new DamageModifierSet
        {
            Coefficients = args.Damage.DamageDict.Keys.ToDictionary(k => k.Id, _ => coeff),
        };
        args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, modSet);
    }
}
