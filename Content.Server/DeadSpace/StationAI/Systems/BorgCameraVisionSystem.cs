// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Power;
using Content.Shared.PowerCell;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Silicons.StationAi;
using Content.Shared.StationAi;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.DeadSpace.StationAI.Systems;

/// <summary>
/// Adjusts station AI vision supplied by cyborg cameras when the cyborg is critical, dead, or unpowered.
/// Empty cyborgs intentionally remain valid cameras.
/// </summary>
public sealed class BorgCameraVisionSystem : EntitySystem
{
    private const float PoweredRange = 4f;
    private const float UnpoweredRange = 2f;
    private const float CriticalVisibleTileChance = 0.95f;
    private static readonly TimeSpan PowerRefreshInterval = TimeSpan.FromSeconds(1);

    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly SharedStationAiSystem _stationAi = default!;

    private TimeSpan _nextPowerRefresh;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationAiVisionComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<StationAiVisionComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<StationAiVisionComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<StationAiVisionComponent, PowerCellChangedEvent>(OnPowerCellChanged);
        SubscribeLocalEvent<StationAiVisionComponent, PowerCellSlotEmptyEvent>(OnPowerCellSlotEmpty);
        SubscribeLocalEvent<StationAiVisionComponent, ChargeChangedEvent>(OnChargeChanged);
        SubscribeLocalEvent<StationAiVisionComponent, BatteryStateChangedEvent>(OnBatteryStateChanged);
    }

    private void OnMapInit(Entity<StationAiVisionComponent> ent, ref MapInitEvent args)
    {
        if (!HasComp<BorgChassisComponent>(ent))
            return;

        RefreshPower(ent);
        RefreshDamageVision(ent);
    }

    private void OnMobStateChanged(Entity<StationAiVisionComponent> ent, ref MobStateChangedEvent args)
    {
        if (!HasComp<BorgChassisComponent>(ent))
            return;

        RefreshDamageVision(ent, args.NewMobState);
    }

    private void OnDamageChanged(Entity<StationAiVisionComponent> ent, ref DamageChangedEvent args)
    {
        if (HasComp<BorgChassisComponent>(ent))
            RefreshDamageVision(ent, damageable: args.Damageable);
    }

    private void OnPowerCellChanged(Entity<StationAiVisionComponent> ent, ref PowerCellChangedEvent args)
    {
        if (HasComp<BorgChassisComponent>(ent))
            RefreshPower(ent);
    }

    private void OnPowerCellSlotEmpty(Entity<StationAiVisionComponent> ent, ref PowerCellSlotEmptyEvent args)
    {
        if (HasComp<BorgChassisComponent>(ent))
            SetRange(ent, UnpoweredRange);
    }

    private void OnChargeChanged(Entity<StationAiVisionComponent> ent, ref ChargeChangedEvent args)
    {
        if (HasComp<BorgChassisComponent>(ent))
            RefreshPower(ent);
    }

    private void OnBatteryStateChanged(Entity<StationAiVisionComponent> ent, ref BatteryStateChangedEvent args)
    {
        if (HasComp<BorgChassisComponent>(ent))
            RefreshPower(ent);
    }

    private void RefreshDamageVision(
        Entity<StationAiVisionComponent> ent,
        MobState? state = null,
        DamageableComponent? damageable = null)
    {
        if (state == null && TryComp<MobStateComponent>(ent, out var mobState))
            state = mobState.CurrentState;

        if (state == MobState.Dead)
        {
            SetVisibleTileChance(ent, 0f);
            return;
        }

        if (!Resolve(ent.Owner, ref damageable, false) ||
            !_mobThreshold.TryGetThresholdForState(ent.Owner, MobState.Critical, out var criticalThreshold) ||
            !_mobThreshold.TryGetThresholdForState(ent.Owner, MobState.Dead, out var deadThreshold))
        {
            SetVisibleTileChance(ent, 1f);
            return;
        }

        var criticalDamage = criticalThreshold.Value.Float();
        var deadDamage = deadThreshold.Value.Float();
        var currentDamage = damageable.TotalDamage.Float();

        if (currentDamage < criticalDamage || deadDamage <= criticalDamage)
        {
            SetVisibleTileChance(ent, 1f);
            return;
        }

        var criticalProgress = Math.Clamp(
            (currentDamage - criticalDamage) / (deadDamage - criticalDamage),
            0f,
            1f);

        SetVisibleTileChance(ent, CriticalVisibleTileChance * (1f - criticalProgress));
    }

    private void SetVisibleTileChance(Entity<StationAiVisionComponent> ent, float chance)
    {
        if (chance >= 1f)
        {
            _stationAi.SetVisionTileVisibility(ent, 1f, 0);
            return;
        }

        var seed = ent.Comp.VisibilitySeed;
        while (seed == 0)
        {
            seed = _random.Next();
        }

        _stationAi.SetVisionTileVisibility(ent, chance, seed);
    }

    private void RefreshPower(Entity<StationAiVisionComponent> ent)
    {
        SetRange(ent, _powerCell.HasDrawCharge(ent.Owner) ? PoweredRange : UnpoweredRange);
    }

    private void SetRange(Entity<StationAiVisionComponent> ent, float range)
    {
        if (ent.Comp.Range == range)
            return;

        _stationAi.SetVisionRange(ent, range);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextPowerRefresh)
            return;

        _nextPowerRefresh = _timing.CurTime + PowerRefreshInterval;

        var query = EntityQueryEnumerator<StationAiVisionComponent, BorgChassisComponent>();
        while (query.MoveNext(out var uid, out var vision, out _))
        {
            RefreshPower((uid, vision));
        }
    }
}
