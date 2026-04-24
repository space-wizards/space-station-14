using System.Linq;
using Content.Server.Stunnable;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Creatures.TheCreature;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Movement.Systems;
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

namespace Content.Server.Creatures.TheCreature;

public sealed class CreatureSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly SharedStealthSystem _stealth = default!;
    [Dependency] private readonly StunSystem _stun = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CreatureComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<CreatureComponent, MobStateChangedEvent>(OnMobStateChanged);

        // Action → open upgrade menu
        SubscribeLocalEvent<CreatureComponent, CreatureUpgradeMenuActionEvent>(OnMenuAction);

        // BUI lifecycle
        Subs.BuiEvents<CreatureComponent>(CreatureUiKey.UpgradeMenu, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnMenuOpened);
            subs.Event<CreatureEvolveMessage>(OnEvolve);
        });

        // Runtime upgrade effects (event-based — no explicit refresh needed)
        SubscribeLocalEvent<CreatureComponent, GetMeleeDamageEvent>(OnGetMeleeDamage);
        SubscribeLocalEvent<CreatureComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
        SubscribeLocalEvent<CreatureComponent, DamageModifyEvent>(OnDamageModify);

        // Sting hit — apply rank-scaled knockdown
        SubscribeLocalEvent<CreatureStingProjectileComponent, ProjectileHitEvent>(OnStingHit);

        // Blood feeding
        SubscribeLocalEvent<CreatureComponent, IngestingEvent>(OnCreatureIngesting);

        // Ravenous - speed up eating DoAfter (broadcast; runs after IngestionSystem sets base time)
        SubscribeLocalEvent<EdibleEvent>(OnCreatureEdible,
            after: [typeof(IngestionSystem)]);
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void OnMapInit(EntityUid uid, CreatureComponent comp, MapInitEvent args)
    {
        ApplyUpgrades(uid, comp);
    }

    private void OnMobStateChanged(EntityUid uid, CreatureComponent comp, MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
            _stealth.SetEnabled(uid, false);
    }

    // ── BUI ───────────────────────────────────────────────────────────────────

    private void OnMenuAction(EntityUid uid, CreatureComponent comp, CreatureUpgradeMenuActionEvent args)
    {
        _uiSystem.TryToggleUi(uid, CreatureUiKey.UpgradeMenu, args.Performer);
    }

    private void OnMenuOpened(EntityUid uid, CreatureComponent comp, BoundUIOpenedEvent args)
    {
        ApplyUpgrades(uid, comp);
        UpdateBuiState(uid, comp);
    }

    private void OnEvolve(EntityUid uid, CreatureComponent comp, CreatureEvolveMessage msg)
    {
        if (!_proto.TryIndex<CreatureUpgradePrototype>(msg.UpgradeId, out var upgrade))
            return;

        comp.UpgradeRanks.TryGetValue(msg.UpgradeId, out var currentRank);

        if (currentRank >= CreatureUpgradePrototype.MaxRank)
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

    public void UpdateBuiState(EntityUid uid, CreatureComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        _uiSystem.SetUiState(uid, CreatureUiKey.UpgradeMenu,
            new CreatureUpgradeMenuBuiState(
                comp.BloodPool,
                comp.MaxBloodPool,
                comp.BloodConsumedTotal,
                new Dictionary<string, int>(comp.UpgradeRanks)));
    }

    // ── Upgrade application ───────────────────────────────────────────────────

    private void ApplyUpgrades(EntityUid uid, CreatureComponent comp)
    {
        comp.UpgradeRanks.TryGetValue("CreatureUpgradeShadow", out var shadowRank);
        ApplyStealthUpgrade(uid, shadowRank);

        comp.UpgradeRanks.TryGetValue("CreatureUpgradePry", out var pryRank);
        ApplyPryUpgrade(uid, pryRank);

        _movement.RefreshMovementSpeedModifiers(uid);
    }

    private void ApplyStealthUpgrade(EntityUid uid, int rank)
    {
        if (!TryComp<StealthOnMoveComponent>(uid, out var stealthMove))
            return;

        if (!_proto.TryIndex<CreatureUpgradePrototype>("CreatureUpgradeShadow", out var upgrade))
            return;

        stealthMove.PassiveVisibilityRate = upgrade.Magnitudes[rank];
    }

    private void ApplyPryUpgrade(EntityUid uid, int rank)
    {
        if (!TryComp<PryingComponent>(uid, out var prying))
            return;

        if (!_proto.TryIndex<CreatureUpgradePrototype>("CreatureUpgradePry", out var upgrade))
            return;

        prying.SpeedModifier = upgrade.Magnitudes[rank];
    }

    // ── Event handlers for runtime upgrade effects ────────────────────────────

    private void OnGetMeleeDamage(EntityUid uid, CreatureComponent comp, ref GetMeleeDamageEvent args)
    {
        comp.UpgradeRanks.TryGetValue("CreatureUpgradePredator", out var rank);
        if (rank == 0)
            return;

        if (!_proto.TryIndex<CreatureUpgradePrototype>("CreatureUpgradePredator", out var upgrade))
            return;

        var coeff = upgrade.Magnitudes[rank];
        args.Modifiers.Add(new DamageModifierSet
        {
            Coefficients = args.Damage.DamageDict.Keys.ToDictionary(k => k.Id, _ => coeff),
        });
    }

    private void OnRefreshSpeed(EntityUid uid, CreatureComponent comp, RefreshMovementSpeedModifiersEvent args)
    {
        comp.UpgradeRanks.TryGetValue("CreatureUpgradeQuickness", out var rank);
        if (rank == 0)
            return;

        if (!_proto.TryIndex<CreatureUpgradePrototype>("CreatureUpgradeQuickness", out var upgrade))
            return;

        var mult = upgrade.Magnitudes[rank];
        args.ModifySpeed(mult, mult);
    }

    private void OnStingHit(EntityUid uid, CreatureStingProjectileComponent comp, ref ProjectileHitEvent args)
    {
        if (args.Shooter is not { } shooter || !TryComp<CreatureComponent>(shooter, out var creature))
            return;

        creature.UpgradeRanks.TryGetValue("CreatureUpgradeVenom", out var rank);

        if (!_proto.TryIndex<CreatureUpgradePrototype>("CreatureUpgradeVenom", out var upgrade))
            return;

        var stunTime = TimeSpan.FromSeconds(upgrade.Magnitudes[rank]);
        _stun.TryKnockdown(args.Target, stunTime, refresh: true, autoStand: true, drop: false, force: true);
    }

    private void OnCreatureEdible(ref EdibleEvent args)
    {
        if (!TryComp<CreatureComponent>(args.User, out var creature))
            return;

        creature.UpgradeRanks.TryGetValue("CreatureUpgradeRavenous", out var rank);
        if (rank == 0)
            return;

        if (!_proto.TryIndex<CreatureUpgradePrototype>("CreatureUpgradeRavenous", out var upgrade))
            return;

        args.Time = TimeSpan.FromSeconds(args.Time.TotalSeconds * upgrade.Magnitudes[rank]);
    }

    private void OnCreatureIngesting(EntityUid uid, CreatureComponent comp, ref IngestingEvent args)
    {
        // Measure blood reagents for the pool, then clear the entire split so nothing is metabolized.
        // Clearing everything prevents toxicity from non-standard blood types and any accompanying reagents.
        var bloodGained = 0f;
        var toRemove = new List<(ReagentId id, FixedPoint2 qty)>();

        foreach (var rq in args.Split.Contents)
        {
            if (IsBloodReagent(rq.Reagent.Prototype))
                bloodGained += (float) rq.Quantity;
            toRemove.Add((rq.Reagent, rq.Quantity));
        }

        foreach (var (id, qty) in toRemove)
            args.Split.RemoveReagent(id, qty);

        if (bloodGained == 0f)
            return;

        // Add to upgrade blood pool resource
        var absorbed = Math.Min(bloodGained, comp.MaxBloodPool - comp.BloodPool);
        comp.BloodPool += absorbed;
        comp.BloodConsumedTotal += absorbed;
        Dirty(uid, comp);
        UpdateBuiState(uid, comp);

        // Replenish the creature's own bloodstream (capped at solution max)
        Entity<SolutionComponent>? bloodSolEnt = null;
        if (_solutionContainer.ResolveSolution((uid, null), BloodstreamComponent.DefaultBloodSolutionName, ref bloodSolEnt))
            _solutionContainer.TryAddReagent(bloodSolEnt.Value, "FerrochromicAcid", FixedPoint2.New(bloodGained));
    }

    private bool IsBloodReagent(string prototypeId)
    {
        if (prototypeId == "Blood")
            return true;
        return _proto.TryIndex<ReagentPrototype>(prototypeId, out var proto)
               && proto.Parents != null
               && proto.Parents.Contains("Blood");
    }

    private void OnDamageModify(EntityUid uid, CreatureComponent comp, DamageModifyEvent args)
    {
        comp.UpgradeRanks.TryGetValue("CreatureUpgradeIronhide", out var rank);
        if (rank == 0)
            return;

        if (!_proto.TryIndex<CreatureUpgradePrototype>("CreatureUpgradeIronhide", out var upgrade))
            return;

        var coeff = upgrade.Magnitudes[rank];
        var modSet = new DamageModifierSet
        {
            Coefficients = args.Damage.DamageDict.Keys.ToDictionary(k => k.Id, _ => coeff),
        };
        args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, modSet);
    }
}
