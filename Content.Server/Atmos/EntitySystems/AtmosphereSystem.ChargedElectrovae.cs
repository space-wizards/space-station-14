using Content.Server.Atmos.Components;
using Content.Server.Power.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using Content.Shared.Power.Components;

namespace Content.Server.Atmos.EntitySystems;

public sealed partial class AtmosphereSystem
{
    // Note: Dependencies and queries are declared in the main AtmosphereSystem.cs file
    // This partial class just adds the charged electrovae processing logic

    // Track original battery capacities for restoration
    private readonly Dictionary<EntityUid, float> _originalBatteryCapacities = [];

    public void ChargedElectrovaeExpose(TileAtmosphere tile, float intensity)
    {
        if (tile == null)
            return;

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
        const float minimumChargedMoles = 0.01f;
        const float intensityDivisor = 2f;

        if (!tile.ChargedEffect.Active)
            return;

        var mixture = tile.Air;
        if (mixture == null)
        {
            tile.ChargedEffect = default;
            return;
        }

        var chargedMoles = mixture.GetMoles(Gas.ChargedElectrovae);
        if (chargedMoles < minimumChargedMoles)
        {
            RestorePowerRequirements(tile);
            tile.ChargedEffect = default;
            return;
        }

        tile.ChargedEffect.Intensity = Math.Min(chargedMoles / intensityDivisor, 1f);

        // Update visual state based on intensity
        const float highIntensityThreshold = 0.75f;  // 1.5+ moles
        const float mediumIntensityThreshold = 0.5f;  // 1.0+ moles
        const float lowIntensityThreshold = 0.25f;    // 0.5+ moles

        tile.ChargedEffect.State = tile.ChargedEffect.Intensity switch
        {
            >= highIntensityThreshold => 3,
            >= mediumIntensityThreshold => 2,
            >= lowIntensityThreshold => 1,
            _ => 0
        };

        const float lookupFlags = 0f;

        _entSet.Clear();
        _lookup.GetLocalEntitiesIntersecting(
            tile.GridIndex, tile.GridIndices, _entSet, lookupFlags);

        foreach (var entity in _entSet)
        {
            // Charge batteries (SMES, APCs, etc.) and expand their capacity (up to 2x)
            if (TryComp<BatteryComponent>(entity, out var battery))
            {
                ChargeBattery(entity, battery, tile.ChargedEffect.Intensity, chargedMoles);
            }

            // Power machines directly (bypass normal power requirement)
            if (_powerReceiverQuery.TryGetComponent(entity, out var receiver))
            {
                ApplyChargedElectrovalePower(receiver, tile.ChargedEffect.Intensity);
            }

            // Lightning strikes on mobs
            if (_mobQuery.HasComponent(entity))
            {
                const float lightningChanceMultiplier = 0.01f;
                var strikeChance = tile.ChargedEffect.Intensity * lightningChanceMultiplier;
                if (_random.Prob(strikeChance))
                {
                    ApplyLightningStrike(entity, tile.ChargedEffect.Intensity);
                }
            }
        }

    }

    /// <summary>
    /// Restores normal power requirements for all machines on a tile
    /// </summary>
    private void RestorePowerRequirements(TileAtmosphere tile)
    {
        const float lookupFlags = 0f;

        _entSet.Clear();
        _lookup.GetLocalEntitiesIntersecting(
            tile.GridIndex, tile.GridIndices, _entSet, lookupFlags);

        foreach (var entity in _entSet)
        {
            if (_powerReceiverQuery.TryGetComponent(entity, out var receiver))
            {
                if (!receiver.NeedsPower)
                    receiver.NeedsPower = true;
            }
        }
    }

    /// <summary>
    /// Charges a battery from charged electrovae gas and expands its capacity (up to 2x)
    /// </summary>
    private void ChargeBattery(EntityUid uid, BatteryComponent battery, float intensity, float chargedMoles)
    {
        const float minimumIntensityToCharge = 0.1f;
        const float chargeRatePerIntensity = 400f; // Watts

        if (intensity < minimumIntensityToCharge)
        {
            // Restore original max charge if we had expanded it
            RestoreBatteryCapacity(uid, battery);
            return;
        }

        // Charge rate scales with intensity (0 to 0.4kW)
        var chargeRate = intensity * chargeRatePerIntensity;
        _battery.ChangeCharge(uid, chargeRate, battery);

        ExpandBatteryCapacity(uid, battery, chargedMoles);
    }

    /// <summary>
    /// Expands battery capacity based on charged electrovae gas concentration
    /// Asymptotically approaches 2x capacity
    /// </summary>
    private void ExpandBatteryCapacity(EntityUid uid, BatteryComponent battery, float chargedMoles)
    {
        const float expansionDecayConstant = 20f; // Controls how quickly we approach 2x capacity

        if (!_originalBatteryCapacities.TryGetValue(uid, out var originalMaxCharge))
        {
            originalMaxCharge = battery.MaxCharge;
            _originalBatteryCapacities[uid] = originalMaxCharge;
        }

        // Calculate expansion multiplier using asymptotic curve
        // 20 moles = 1.63x, 40 moles = 1.86x, 80 moles = 1.98x, 200 moles = 1.9999x
        var expansionMultiplier = 1f + (1f - MathF.Exp(-chargedMoles / expansionDecayConstant));

        var newMaxCharge = originalMaxCharge * expansionMultiplier;
        _battery.SetMaxCharge(uid, newMaxCharge, battery);
    }

    /// <summary>
    /// Restores battery capacity to its original value.
    /// </summary>
    private void RestoreBatteryCapacity(EntityUid uid, BatteryComponent battery)
    {
        if (_originalBatteryCapacities.TryGetValue(uid, out var originalMaxCharge))
        {
            _battery.SetMaxCharge(uid, originalMaxCharge, battery);
        }
    }

    /// <summary>
    /// Applies power to a machine from charged electrovae gas.
    /// This bypasses normal APC power requirements.
    /// </summary>
    private static void ApplyChargedElectrovalePower(
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
