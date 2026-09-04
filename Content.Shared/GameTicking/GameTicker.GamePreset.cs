using Content.Shared.GameTicking.Prototypes;
using Content.Shared.GameTicking.Components;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared.GameTicking;

public abstract partial class GameTicker
{

    /// <inhereitdoc cref="GetMinimumPlayerCount(GamePresetPrototype)"/>
    [PublicAPI]
    public int GetMinimumPlayerCount(ProtoId<GamePresetPrototype> proto)
    {
        if (!ProtoMan.Resolve(proto, out var preset))
            return 0;

        return GetMinimumPlayerCount(preset);
    }

    /// <summary>
    /// Gets the minimum number of players required for a game preset to start.
    /// Checks both the preset itself, and all rules to find the minimum.
    /// </summary>
    /// <param name="proto">Game preset prototype we're checking.</param>
    /// <returns>Minimum number of players required for the rule to start.</returns>
    [PublicAPI]
    public int GetMinimumPlayerCount(GamePresetPrototype proto)
    {
        var min = proto.MinPlayers ?? 0;
        foreach (var entProto in proto.Rules)
        {
            if (!ProtoMan.Resolve(entProto, out var ent))
                continue;

            if (!ent.TryComp<GameRuleComponent>(out var rule, Factory))
                continue;

            min = Math.Max(min, rule.MinPlayers);
        }

        return min;
    }
}
