using Content.Server.Atmos.Components;
using Content.Server.Power.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Database;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Server.Atmos.EntitySystems;

public sealed partial class AtmosphereSystem
{
    // Note: Dependencies and queries are declared in the main AtmosphereSystem.cs file
    // This partial class just adds the charged electrovae processing logic

    private void InitializeChargedElectrovae()
    {
        SubscribeLocalEvent<BatteryComponent, ComponentShutdown>(OnBatteryShutdown);
        SubscribeLocalEvent<PredictedBatteryComponent, ComponentShutdown>(OnPredictedBatteryShutdown);
        SubscribeLocalEvent<ChargedElectrovaeAffectedComponent, ComponentShutdown>(OnChargedElectrovaeAffectedShutdown);
        SubscribeLocalEvent<ChargedElectrovaeAffectedComponent, RefreshChargeRateEvent>(OnRefreshChargeRate);
    }

    /// <summary>
    /// Handles cleanup when a battery component is removed.
    /// Ensures the ChargedElectrovaeAffectedComponent is also removed to prevent stale data.
    /// </summary>
    private void OnBatteryShutdown(Entity<BatteryComponent> ent, ref ComponentShutdown args)
    {
        RemCompDeferred<ChargedElectrovaeAffectedComponent>(ent);
    }

    /// <summary>
    /// Handles cleanup when a predicted battery component is removed.
    /// Ensures the ChargedElectrovaeAffectedComponent is also removed to prevent stale data.
    /// </summary>
    private void OnPredictedBatteryShutdown(Entity<PredictedBatteryComponent> ent, ref ComponentShutdown args)
    {
        RemCompDeferred<ChargedElectrovaeAffectedComponent>(ent);
    }

    /// <summary>
    /// Handles charge rate refresh for batteries in charged electrovae gas.
    /// Adds additional charge rate based on gas concentration and intensity.
    /// </summary>
    private void OnRefreshChargeRate(Entity<ChargedElectrovaeAffectedComponent> ent, ref RefreshChargeRateEvent args)
    {
        // Check if entity is in charged electrovae gas
        var mixture = GetTileMixture((ent.Owner, null));
        if (mixture == null)
            return;

        var chargedMoles = mixture.GetMoles(Gas.ChargedElectrovae);
        if (chargedMoles < Atmospherics.ChargedElectrovaeMinimumMoles)
            return;

        // Calculate intensity and charge rate
        var intensity = Math.Min(chargedMoles / Atmospherics.ChargedElectrovaeIntensityDivisor, 1f);
        const float minimumIntensityToCharge = 0.1f;
        const float chargeRatePerIntensity = 400f; // Watts

        if (intensity >= minimumIntensityToCharge)
        {
            // Add charge rate based on intensity
            args.NewChargeRate += intensity * chargeRatePerIntensity;
        }
    }

    /// <summary>
    /// Handles cleanup when an entity is no longer affected by charged electrovae gas.
    /// Restores battery capacity and power requirements to their original states.
    /// </summary>
    private void OnChargedElectrovaeAffectedShutdown(Entity<ChargedElectrovaeAffectedComponent> ent, ref ComponentShutdown args)
    {
        // Restore battery capacity if this entity had it expanded
        if (_batteryQuery.TryGetComponent(ent, out var battery))
            RestoreBatteryCapacity((ent, battery, ent.Comp));

        // Restore power requirements if this entity had them bypassed
        if (_powerReceiverQuery.TryGetComponent(ent, out var receiver))
        {
            if (!receiver.NeedsPower)
                receiver.NeedsPower = true;
        }
    }

    public void ChargedElectrovaeExpose(TileAtmosphere tile, EntityUid gridUid, float intensity)
    {
        if (!_atmosQuery.TryGetComponent(gridUid, out var atmosphere))
            return;

        if (!tile.ChargedEffect.Active)
            atmosphere.ChargedElectrovaeTiles.Add(tile);

        tile.ChargedEffect.Active = true;
        tile.ChargedEffect.Intensity = Math.Clamp(intensity, 0f, 1f);
    }

    /// <summary>
    /// Processes charged electrovae effects on a tile
    /// This follows the same pattern as hotspot processing
    /// </summary>
    private void ProcessChargedElectrovae(
        Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent> ent,
        TileAtmosphere tile)
    {
        var atmosphere = ent.Comp1;

        if (!tile.ChargedEffect.Active)
            return;

        var mixture = tile.Air;
        if (mixture == null)
        {
            tile.ChargedEffect = default;
            atmosphere.ChargedElectrovaeTiles.Remove(tile);
            return;
        }

        var chargedMoles = mixture.GetMoles(Gas.ChargedElectrovae);
        if (chargedMoles < Atmospherics.ChargedElectrovaeMinimumMoles)
        {
            RestorePowerRequirements(tile);
            tile.ChargedEffect = default;
            atmosphere.ChargedElectrovaeTiles.Remove(tile);
            return;
        }

        tile.ChargedEffect.Intensity = Math.Min(chargedMoles / Atmospherics.ChargedElectrovaeIntensityDivisor, 1f);

        // Update visual state based on intensity
        tile.ChargedEffect.State = tile.ChargedEffect.Intensity switch
        {
            >= Atmospherics.ChargedElectrovaeHighIntensityThreshold => 3,
            >= Atmospherics.ChargedElectrovaeMediumIntensityThreshold => 2,
            >= Atmospherics.ChargedElectrovaeLowIntensityThreshold => 1,
            _ => 0,
        };

        _entSet.Clear();
        _lookup.GetLocalEntitiesIntersecting(
            tile.GridIndex,
            tile.GridIndices,
            _entSet,
            0f);

        foreach (var entity in _entSet)
        {
            // Mark entity as affected by charged electrovae
            EnsureComp<ChargedElectrovaeAffectedComponent>(entity);

            // Handle batteries - expand capacity and trigger charge rate refresh
            if (_batteryQuery.HasComponent(entity) || _predictedBatteryQuery.HasComponent(entity))
            {
                ProcessBattery(entity, tile.ChargedEffect.Intensity, chargedMoles);
            }

            // Power machines directly (bypass normal power requirement)
            if (_powerReceiverQuery.TryGetComponent(entity, out var receiver))
            {
                ApplyChargedElectrovaePower(receiver, tile.ChargedEffect.Intensity);
            }

            // Lightning strikes on mobs
            if (_mobQuery.HasComponent(entity))
            {
                var strikeChance = tile.ChargedEffect.Intensity * Atmospherics.ChargedElectrovaeLightningChanceMultiplier;
                if (_random.Prob(strikeChance))
                {
                    ApplyLightningStrike(entity, tile.ChargedEffect.Intensity);
                }
            }
        }

    }

    /// <summary>
    /// Removes charged electrovae effects from all entities on a tile
    /// </summary>
    private void RestorePowerRequirements(TileAtmosphere tile)
    {
        _entSet.Clear();
        _lookup.GetLocalEntitiesIntersecting(
            tile.GridIndex,
            tile.GridIndices,
            _entSet,
            0f);

        foreach (var entity in _entSet)
        {
            // Remove the affected component, which will trigger cleanup via the event handler
            RemComp<ChargedElectrovaeAffectedComponent>(entity);
        }
    }

    /// <summary>
    /// Cleans up entities that are no longer in charged electrovae gas.
    /// Should be called after processing all charged electrovae tiles.
    /// </summary>
    public void CleanupChargedElectrovaeEntities(Entity<GridAtmosphereComponent> grid)
    {
        var query = EntityQueryEnumerator<ChargedElectrovaeAffectedComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var transform))
        {
            if (transform.GridUid != grid.Owner)
                continue;

            var mixture = GetTileMixture((uid, transform));
            if (mixture == null)
            {
                RemComp<ChargedElectrovaeAffectedComponent>(uid);
                continue;
            }

            var chargedMoles = mixture.GetMoles(Gas.ChargedElectrovae);
            if (chargedMoles < Atmospherics.ChargedElectrovaeMinimumMoles)
            {
                RemComp<ChargedElectrovaeAffectedComponent>(uid);
            }
        }
    }

    /// <summary>
    /// Processes a battery in charged electrovae gas.
    /// Expands battery capacity asymptotically and refreshes charge rate for predicted batteries.
    /// </summary>
    private void ProcessBattery(EntityUid uid, float intensity, float chargedMoles)
    {
        const float minimumIntensityToCharge = 0.1f;

        if (intensity < minimumIntensityToCharge)
        {
            // Restore original max charge if we had expanded it
            if (_batteryQuery.TryGetComponent(uid, out var battery) &&
                TryComp<ChargedElectrovaeAffectedComponent>(uid, out var affected))
                RestoreBatteryCapacity((uid, battery, affected));
            return;
        }

        // Expand battery capacity based on charged moles
        if (_batteryQuery.TryGetComponent(uid, out var batteryComp) &&
            TryComp<ChargedElectrovaeAffectedComponent>(uid, out var affectedComp))
            ExpandBatteryCapacity((uid, batteryComp, affectedComp), chargedMoles);

        // Trigger charge rate refresh for PredictedBatteryComponent
        // The RefreshChargeRateEvent handler will add the appropriate charge rate
        if (_predictedBatteryQuery.HasComponent(uid))
            _predictedBattery.RefreshChargeRate(uid);
    }

    /// <summary>
    /// Expands battery capacity based on charged electrovae gas concentration.
    /// Uses an asymptotic exponential curve that approaches 2x capacity at high concentrations.
    /// Formula: multiplier = 1 + (1 - e^(-moles/20))
    /// At 20 moles: 1.63x, 40 moles: 1.86x, 80 moles: 1.98x, 200 moles: 1.9999x
    /// </summary>
    private void ExpandBatteryCapacity(
        Entity<BatteryComponent, ChargedElectrovaeAffectedComponent> ent,
        float chargedMoles)
    {
        const float expansionDecayConstant = 20f; // Controls how quickly we approach 2x capacity

        if (ent.Comp2.OriginalBatteryMaxCharge == null)
        {
            ent.Comp2.OriginalBatteryMaxCharge = ent.Comp1.MaxCharge;

            _adminLog.Add(LogType.AtmosPowerChanged, LogImpact.Low,
                $"Battery {ToPrettyString(ent)} capacity expanded by charged electrovae from {ent.Comp1.MaxCharge:F0}W to potentially 2x");
        }

        // Calculate expansion multiplier using asymptotic curve
        var expansionMultiplier = 1f + (1f - MathF.Exp(-chargedMoles / expansionDecayConstant));

        var newMaxCharge = ent.Comp2.OriginalBatteryMaxCharge.Value * expansionMultiplier;
        _battery.SetMaxCharge((ent.Owner, ent.Comp1), newMaxCharge);
    }

    /// <summary>
    /// Restores battery capacity to its original value before charged electrovae expansion.
    /// Clears the stored original capacity after restoration.
    /// </summary>
    private void RestoreBatteryCapacity(
        Entity<BatteryComponent, ChargedElectrovaeAffectedComponent> ent)
    {
        if (ent.Comp2.OriginalBatteryMaxCharge != null)
        {
            _battery.SetMaxCharge((ent.Owner, ent.Comp1), ent.Comp2.OriginalBatteryMaxCharge.Value);
            ent.Comp2.OriginalBatteryMaxCharge = null;
        }
    }

    /// <summary>
    /// Applies power to a machine from charged electrovae gas.
    /// This bypasses normal APC power requirements.
    /// </summary>
    private static void ApplyChargedElectrovaePower(
        ApcPowerReceiverComponent receiver,
        float intensity)
    {
        const float minimumIntensityToPower = 0.1f;

        if (intensity < minimumIntensityToPower)
        {
            if (!receiver.NeedsPower)
                receiver.NeedsPower = true;
            return;
        }

        receiver.NeedsPower = false;
    }

    /// <summary>
    /// Applies a lightning strike to an entity from charged electrovae
    /// </summary>
    private void ApplyLightningStrike(EntityUid target, float intensity)
    {
        // Scale damage and stun time based on intensity
        // At low intensity: 5 damage, 1 second stun
        // At high intensity: 10 damage, 3 seconds stun
        const int baseDamage = 5;
        const int damagePerIntensity = 5;
        const float baseStunSeconds = 1f;
        const float stunSecondsPerIntensity = 2f;

        var damage = (int)(baseDamage + intensity * damagePerIntensity);
        var stunTime = TimeSpan.FromSeconds(baseStunSeconds + intensity * stunSecondsPerIntensity);

        _adminLog.Add(LogType.Electrocution,
            LogImpact.Medium,
            $"{ToPrettyString(target):target} struck by charged electrovae lightning ({damage} damage, {stunTime.TotalSeconds:F1}s stun, intensity {intensity:F2})");

        _electrocution.TryDoElectrocution(
            target,
            null,
            damage,
            stunTime,
            refresh: true,
            siemensCoefficient: 1f,
            ignoreInsulation: false
        );
    }
}
