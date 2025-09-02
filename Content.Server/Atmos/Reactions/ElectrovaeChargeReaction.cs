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
///     Chemical Equation: H₄N₂₂O₁₁ → H₄N₂₂O₁₁⁺
///     This reaction searches for cables in the tile containing electrovae gas and
///     drains power from them to converts that power into charged electrovae gas.
///     Higher voltage cables makes the reaction more efficient.
/// </summary>
[UsedImplicitly]
public sealed partial class ElectrovaeChargeReaction : IGasReactionEffect
{
    // Track the last reaction time for each tile to throttle it
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

        // Throttle reaction checks to prevent excessive processing
        var tileKey = (tileAtmos.GridIndex, tileAtmos.GridIndices);
        if (LastPowerCheckTimes.TryGetValue(tileKey, out var lastTime) &&
            currentTime - lastTime < PowerCheckInterval)
        {
            return ReactionResult.NoReaction;
        }
        LastPowerCheckTimes[tileKey] = currentTime;

        var tileRef = atmosphereSystem.GetTileRef(tileAtmos);
        if (tileRef == default)
            return ReactionResult.NoReaction;

        // Find all cable entities in the search radius - need electrical energy for the reaction
        var centerPos = mapSystem.ToCenterCoordinates(tileRef);
        var cableEntities = lookup.GetEntitiesInRange(centerPos, 0.5f)
            .Where(entMan.HasComponent<CableComponent>)
            .ToList();

        var cablesFound = cableEntities.Count;
        if (cablesFound == 0) return ReactionResult.NoReaction;

        var totalPowerDrained = 0f;
        var highVoltageCables = 0;
        var mediumVoltageCables = 0;

        // More electrovae means more power can be drained
        var desiredPowerDrain = Math.Clamp(initialE * Atmospherics.ElectrovaeChargePowerDrainPerMole,
            Atmospherics.ElectrovaeChargeMinimumPowerDrain,
            Atmospherics.ElectrovaeChargeMaximumPowerDrain);

        // Try to drain power from each cable
        foreach (var cable in cableEntities)
        {
            var cableComp = entMan.GetComponent<CableComponent>(cable);

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
                CableType.HighVoltage => 1.5f,
                CableType.MediumVoltage => 1.2f,
                _ => 1.0f
            };

            var powerToDraw = desiredPowerDrain * powerMultiplier / cablesFound;
            tempConsumer.DrawRate = powerToDraw;

            // Calculate actual power drained with efficiency factor
            totalPowerDrained += powerToDraw * 0.85f; // 85% efficiency

            entMan.RemoveComponent<PowerConsumerComponent>(cable);
        }

        if (totalPowerDrained < Atmospherics.ElectrovaeChargeMinimumPowerDrain)
            return ReactionResult.NoReaction;

        var chargedElectrovaeProduced = totalPowerDrained * Atmospherics.ElectrovaeChargeConversionEfficiency / Atmospherics.ElectrovaeChargePowerDrainPerMole;

        mixture.AdjustMoles(Gas.Electrovae, -chargedElectrovaeProduced);
        mixture.AdjustMoles(Gas.ChargedElectrovae, chargedElectrovaeProduced);

        // Heat up the mixture slightly due to electrical resistance
        mixture.Temperature += chargedElectrovaeProduced * 5f;

        return ReactionResult.Reacting;
    }
}
