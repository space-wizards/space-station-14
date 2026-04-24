using System.Linq;
using Content.Shared.Actions;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Creatures.TheCreature;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Nutrition;
using Content.Shared.Prying.Components;
using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.Creatures.TheCreature;

public sealed class CreatureSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedStealthSystem _stealth = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    // Per-rank multiplier tables (index = rank, 0 = unpurchased)
    private static readonly float[] AttackMultipliers    = { 1.00f, 1.15f, 1.30f, 1.50f };
    private static readonly float[] SpeedMultipliers     = { 1.00f, 1.08f, 1.16f, 1.25f };
    private static readonly float[] ArmorCoefficients    = { 1.00f, 0.90f, 0.80f, 0.70f };
    private static readonly float[] PassiveDecayRates    = { -0.15f, -0.19f, -0.24f, -1.00f };
    private static readonly float[] PrySpeedModifiers    = { 1.00f, 0.75f, 0.40f, 0.05f };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CreatureComponent, MapInitEvent>(OnMapInit);

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

        // Blood feeding
        SubscribeLocalEvent<CreatureComponent, IngestingEvent>(OnCreatureIngesting);
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void OnMapInit(EntityUid uid, CreatureComponent comp, MapInitEvent args)
    {
        ApplyUpgrades(uid, comp);
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
        if (!CreatureUpgradeData.ById.TryGetValue(msg.UpgradeId, out var upgrade))
            return;

        comp.UpgradeRanks.TryGetValue(msg.UpgradeId, out var currentRank);

        if (currentRank >= CreatureUpgradeData.MaxRank)
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

    /// <summary>
    ///     Apply all upgrades that need explicit component mutation.
    ///     Event-based upgrades (attack, armor, speed) are handled by their own subscribers.
    /// </summary>
    private void ApplyUpgrades(EntityUid uid, CreatureComponent comp)
    {
        comp.UpgradeRanks.TryGetValue("shadow", out var shadowRank);
        ApplyStealthUpgrade(uid, shadowRank);

        comp.UpgradeRanks.TryGetValue("pry", out var pryRank);
        ApplyPryUpgrade(uid, pryRank);

        comp.UpgradeRanks.TryGetValue("sting", out var stingRank);
        ApplyStingUpgrade(uid, stingRank);

        // Movement speed is event-driven but needs an explicit refresh after a rank change.
        _movement.RefreshMovementSpeedModifiers(uid);
    }

    private void ApplyStealthUpgrade(EntityUid uid, int rank)
    {
        if (!TryComp<StealthOnMoveComponent>(uid, out var stealthMove))
            return;

        stealthMove.PassiveVisibilityRate = PassiveDecayRates[rank];

        _stealth.SetEnabled(uid, rank >= 1);
    }

    private void ApplyPryUpgrade(EntityUid uid, int rank)
    {
        if (!TryComp<PryingComponent>(uid, out var prying))
            return;

        prying.SpeedModifier = PrySpeedModifiers[rank];
    }

    private void ApplyStingUpgrade(EntityUid uid, int rank)
    {
        if (!TryComp<ActionGunComponent>(uid, out var actionGun) || actionGun.ActionEntity is not { } ae)
            return;

        _actions.SetEnabled(ae, rank >= 1);
    }

    // ── Event handlers for runtime upgrade effects ────────────────────────────

    private void OnGetMeleeDamage(EntityUid uid, CreatureComponent comp, ref GetMeleeDamageEvent args)
    {
        comp.UpgradeRanks.TryGetValue("predator", out var rank);
        if (rank == 0)
            return;

        var coeff = AttackMultipliers[rank];
        args.Modifiers.Add(new DamageModifierSet
        {
            Coefficients = args.Damage.DamageDict.Keys.ToDictionary(k => k.Id, _ => coeff),
        });
    }

    private void OnRefreshSpeed(EntityUid uid, CreatureComponent comp, RefreshMovementSpeedModifiersEvent args)
    {
        comp.UpgradeRanks.TryGetValue("quickness", out var rank);
        if (rank == 0)
            return;

        args.ModifySpeed(SpeedMultipliers[rank], SpeedMultipliers[rank]);
    }

    private void OnCreatureIngesting(EntityUid uid, CreatureComponent comp, ref IngestingEvent args)
    {
        var bloodGained = 0;
        foreach (var rq in args.Split.Contents)
        {
            if (IsBloodReagent(rq.Reagent.Prototype))
                bloodGained += (int) rq.Quantity;
        }

        if (bloodGained == 0)
            return;

        var absorbed = Math.Min(bloodGained, comp.MaxBloodPool - comp.BloodPool);
        comp.BloodPool += absorbed;
        comp.BloodConsumedTotal += absorbed;
        Dirty(uid, comp);
        UpdateBuiState(uid, comp);
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
        comp.UpgradeRanks.TryGetValue("ironhide", out var rank);
        if (rank == 0)
            return;

        var coeff = ArmorCoefficients[rank];
        var modSet = new DamageModifierSet
        {
            Coefficients = args.Damage.DamageDict.Keys.ToDictionary(k => k.Id, _ => coeff),
        };
        args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, modSet);
    }
}
