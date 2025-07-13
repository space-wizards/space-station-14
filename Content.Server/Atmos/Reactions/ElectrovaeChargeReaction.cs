using Content.Server.Atmos.EntitySystems;
using Content.Server.Power.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using Content.Shared.Power;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Server.Atmos.Reactions;

/// <summary>
///     Creates charged electrovae by draining power from nearby electrical cables.
///     This reaction searches for cables in a 3x3 area around tiles containing electrovae gas,
///     drains power from them, and converts that power into charged electrovae gas.
///     Higher voltage cables provide more power and efficiency bonuses.
/// </summary>
[UsedImplicitly]
public sealed partial class ElectrovaeChargeReaction : IGasReactionEffect
{
    // Track when we last checked each tile for power drain
    // This prevents excessive power drain checks every frame and throttles the reaction
    private static readonly Dictionary<(EntityUid Grid, Vector2i Position), TimeSpan> LastPowerCheckTimes = [];

    // Only check for power drain every 2 seconds per tile
    private static readonly TimeSpan PowerCheckInterval = TimeSpan.FromSeconds(2);

    public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale)
    {
        var initialE = mixture.GetMoles(Gas.Electrovae);

        if (initialE < 0.01f || holder is not TileAtmosphere tileAtmos) return ReactionResult.NoReaction;

        var entMan = IoCManager.Resolve<IEntityManager>();
        var mapSystem = entMan.System<SharedMapSystem>();
        var lookup = entMan.System<EntityLookupSystem>();
        var transform = entMan.System<TransformSystem>();
        var timing = IoCManager.Resolve<IGameTiming>();

        var currentTime = timing.CurTime;

        // Create a unique key for this specific tile to track its timing
        var tileKey = (tileAtmos.GridIndex, tileAtmos.GridIndices);

        // Check if we've recently processed this tile and skip if so
        // This throttles the reaction to prevent excessive power drain and performance impact
        if (LastPowerCheckTimes.TryGetValue(tileKey, out var lastTime) &&
            currentTime - lastTime < PowerCheckInterval)
        {
            return ReactionResult.NoReaction;
        }

        LastPowerCheckTimes[tileKey] = currentTime;

        var tileRef = atmosphereSystem.GetTileRef(tileAtmos);
        if (tileRef == default)
            return ReactionResult.NoReaction;

        var centerPos = mapSystem.ToCenterCoordinates(tileRef);

        // Find all cable entities in the search radius
        var cableEntities = lookup.GetEntitiesInRange(centerPos, Atmospherics.ElectrovaeChargeSearchRadius)
            .Where(entMan.HasComponent<CableComponent>)
            .ToList();

        if (cableEntities.Count == 0) return ReactionResult.NoReaction;

        // Track how much power we've drained and what types of cables we found
        var totalPowerDrained = 0f;
        var cablesFound = 0;
        var highVoltageCables = 0;
        var mediumVoltageCables = 0;

        // Calculate how much power we want to drain based on electrovae amount
        // More electrovae means more power can be drained
        var desiredPowerDrain = Math.Clamp(initialE * Atmospherics.ElectrovaeChargePowerDrainPerMole,
            Atmospherics.ElectrovaeChargeMinimumPowerDrain,
            Atmospherics.ElectrovaeChargeMaximumPowerDrain);

        // Try to drain power from each cable
        foreach (var cable in cableEntities)
        {
            if (!entMan.TryGetComponent<CableComponent>(cable, out var cableComp))
                continue;

            cablesFound++;

            switch (cableComp.CableType)
            {
                case CableType.HighVoltage:
                    highVoltageCables++;
                    break;
                case CableType.MediumVoltage:
                    mediumVoltageCables++;
                    break;
            }

            // Create a temporary power consumer on the cable to drain power
            var tempConsumer = entMan.EnsureComponent<PowerConsumerComponent>(cable);

            // Higher voltage cables provide more power
            var powerMultiplier = cableComp.CableType switch
            {
                CableType.HighVoltage => 3.0f,
                CableType.MediumVoltage => 1.5f,
                _ => 1.0f
            };

            var powerToDraw = desiredPowerDrain * powerMultiplier / cablesFound;
            tempConsumer.DrawRate = powerToDraw;

            totalPowerDrained = powerToDraw * 0.69f; // 69% of efficiency (max 207K)

            entMan.RemoveComponent<PowerConsumerComponent>(cable);
        }

        if (totalPowerDrained < 1f)
            return ReactionResult.NoReaction;

        var chargedElectrovaeProduced = totalPowerDrained * Atmospherics.ElectrovaeChargeConversionEfficiency / Atmospherics.ElectrovaeChargePowerDrainPerMole;

        if (highVoltageCables > 0)
            chargedElectrovaeProduced *= 1.5f;  // 50% bonus for HV cables (max 4.14)
        else if (mediumVoltageCables > 0)
            chargedElectrovaeProduced *= 1.2f;  // 20% bonus for MV cables (max 3.31)

        mixture.AdjustMoles(Gas.Electrovae, -chargedElectrovaeProduced);
        mixture.AdjustMoles(Gas.ChargedElectrovae, chargedElectrovaeProduced);

        // Heat up the mixture slightly due to electrical resistance
        mixture.Temperature += chargedElectrovaeProduced * 5f;

        return ReactionResult.Reacting;
    }
}
