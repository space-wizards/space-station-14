using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using JetBrains.Annotations;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Atmos.Reactions;

/// <summary>
///     Creates EMP pulses from charged electrovae gas.
///     Uses ChargedElectrovaeManager for batched processing.
/// </summary>
[UsedImplicitly]
public sealed partial class ChargedElectrovaeEmpReaction : IGasReactionEffect
{
    // Track the last reaction time for each tile to throttle it
    private static readonly Dictionary<(EntityUid Grid, Vector2i Position), TimeSpan> LastReactionTimes = [];

    public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale)
    {
        var initialCE = mixture.GetMoles(Gas.ChargedElectrovae);

        if (initialCE < Atmospherics.ChargedElectrovaeMinimumAmount) return ReactionResult.NoReaction;
        if (holder is not TileAtmosphere tileAtmos) return ReactionResult.NoReaction;

        var timing = IoCManager.Resolve<IGameTiming>();
        var tileKey = (tileAtmos.GridIndex, tileAtmos.GridIndices);
        var currentTime = timing.CurTime;
        if (LastReactionTimes.TryGetValue(tileKey, out var lastTime) &&
            (currentTime - lastTime).TotalSeconds < Atmospherics.ChargedElectrovaeCooldown)
        {
            return ReactionResult.NoReaction;
        }

        // Calculate reaction intensity (0.2-1.0)
        var intensity = Math.Min(initialCE / 10f, 1f);

        // Random chance to react based on concentration (0.2%-1%)
        var random = IoCManager.Resolve<IRobustRandom>();
        var reactionChance = Atmospherics.ChargedElectrovaeEmpChance * intensity;
        if (!random.Prob(reactionChance)) return ReactionResult.NoReaction;

        // Gas consumption will be handled by the manager
        // Register for batch processing
        var entMan = IoCManager.Resolve<IEntityManager>();
        var manager = entMan.System<ChargedElectrovaeManager>();
        manager.RegisterTile(tileAtmos.GridIndex, tileAtmos.GridIndices, intensity);

        // Update the last reaction time for this tile
        LastReactionTimes[tileKey] = currentTime;

        return ReactionResult.Reacting;
    }
}