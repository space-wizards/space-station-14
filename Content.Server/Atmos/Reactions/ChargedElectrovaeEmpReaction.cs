using Content.Server.Atmos.EntitySystems;
using Content.Server.Electrocution;
using Content.Server.Emp;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using Content.Shared.Mobs.Systems;
using JetBrains.Annotations;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Atmos.Reactions;

/// <summary>
///     Creates EMP pulses from charged electrovae gas.
///     Randomly triggers EMP effects and shocks nearby entities based on gas concentration.
///     Higher concentrations of charged electrovae result in more frequent and powerful effects.
/// </summary>
[UsedImplicitly]
public sealed partial class ChargedElectrovaeEmpReaction : IGasReactionEffect
{
    // Track when each tile last emitted an EMP pulse
    private static readonly Dictionary<(EntityUid Grid, Vector2i Position), TimeSpan> LastEmpTimes = [];

    // Base interval between EMP pulses (8 seconds)
    private static readonly TimeSpan BaseEmpInterval = TimeSpan.FromSeconds(8);

    public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale)
    {
        var initialCE = mixture.GetMoles(Gas.ChargedElectrovae);
        var initialO = mixture.GetMoles(Gas.Oxygen);

        // Not enough charged electrovae to do anything
        if (initialCE < Atmospherics.ChargedElectrovaeMinimumAmount)
            return ReactionResult.NoReaction;

        // Only process for tile atmospheres (not portable canisters)
        if (holder is not TileAtmosphere tileAtmos)
            return ReactionResult.NoReaction;

        var entMan = IoCManager.Resolve<IEntityManager>();
        var mapSystem = entMan.System<SharedMapSystem>();
        var empSystem = entMan.System<EmpSystem>();
        var timing = IoCManager.Resolve<IGameTiming>();
        var random = IoCManager.Resolve<IRobustRandom>();

        var currentTime = timing.CurTime;
        var tileKey = (tileAtmos.GridIndex, tileAtmos.GridIndices);

        var concentrationFactor = Math.Min(initialCE / 10f, 1f);

        // Randomize the interval between pulses based on concentration
        var randomizedInterval = BaseEmpInterval * (1.5f - 0.5f * concentrationFactor);

        // Check if enough time has passed since the last EMP on this tile
        if (LastEmpTimes.TryGetValue(tileKey, out var lastTime) &&
            currentTime - lastTime < randomizedInterval)
        {
            return ReactionResult.NoReaction;
        }

        // Random chance to trigger based on concentration
        // This adds unpredictability to the effect and prevents every tile from pulsing at once
        // Base chance is 20%, increasing up to 50% with maximum concentration
        var triggerChance = Atmospherics.ChargedElectrovaeBaseReactionChance + concentrationFactor * 0.3f;
        if (!random.Prob(triggerChance)) return ReactionResult.NoReaction;

        var tileRef = atmosphereSystem.GetTileRef(tileAtmos);
        if (tileRef == default) return ReactionResult.NoReaction;

        var centerPos = mapSystem.ToCenterCoordinates(tileRef);
        var consumeAmount = Math.Min(initialCE, Atmospherics.ChargedElectrovaeMinimumAmount);
        // This ensures effects are proportional to the amount of gas available
        var powerScale = consumeAmount / Atmospherics.ChargedElectrovaeMinimumAmount;

        // Random chance to trigger an EMP pulse based on power scale
        if (random.Prob(Atmospherics.ChargedElectrovaeEmpChance))
        {
            var empRadius = Atmospherics.ChargedElectrovaeEmpRadius * powerScale;
            var empEnergy = Atmospherics.ChargedElectrovaeEmpEnergy * random.NextFloat(0.9f, 1.1f) * powerScale;
            var empStun = Atmospherics.ChargedElectrovaeEmpStunDuration * random.NextFloat(0.9f, 1.1f) * powerScale;

            empSystem.EmpPulse(centerPos, empRadius, empEnergy, empStun);

            var oConsumed = Math.Min(initialO * Atmospherics.ChargedElectrovaeOxygenEmpRatio, consumeAmount * Atmospherics.ChargedElectrovaeOxygenEmpRatio);

            mixture.AdjustMoles(Gas.ChargedElectrovae, -consumeAmount);
            mixture.AdjustMoles(Gas.Oxygen, -oConsumed);

            // Random chance to trigger electrical shocks to nearby entities
            if (random.Prob(Atmospherics.ChargedElectrovaeShockChance))
            {
                // Get required systems for electrocution and entity detection
                var shockSystem = entMan.System<ElectrocutionSystem>();
                var mobState = entMan.System<MobStateSystem>();
                var shockQuery = entMan.GetEntityQuery<PhysicsComponent>();
                var lookup = entMan.System<EntityLookupSystem>();

                var shockRadius = Atmospherics.ChargedElectrovaeShockRadius * random.NextFloat(0.9f, 1.1f) * powerScale;

                foreach (var entity in lookup.GetEntitiesInRange(centerPos, shockRadius))
                {
                    if (!shockQuery.TryGetComponent(entity, out var physics) || !physics.CanCollide)
                        continue;

                    if (mobState.IsAlive(entity) || mobState.IsCritical(entity))
                    {
                        var damage = (int)(Atmospherics.ChargedElectrovaeShockDamage * random.NextFloat(0.8f, 1.2f) * powerScale);
                        var duration = TimeSpan.FromSeconds(Atmospherics.ChargedElectrovaeShockDuration * random.NextFloat(0.7f, 1.3f) * powerScale);

                        shockSystem.TryDoElectrocution(
                            entity,
                            null,
                            damage,
                            duration,
                            true,
                            1f,
                            null,
                            random.Prob(0.2f)
                        );
                    }
                }
            }
        }

        // This prevents the same tile from triggering again immediately
        LastEmpTimes[tileKey] = currentTime;

        return ReactionResult.Reacting;
    }
}