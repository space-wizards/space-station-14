#nullable enable
using System.Collections.Generic;
using Content.IntegrationTests.Fixtures.Attributes;
using Robust.Shared.Configuration;

namespace Content.IntegrationTests.Fixtures;

// REMARK: You may be wondering why this doesn't bother storing the old CVars.
//         This is because TestPair actually has some not-well-known functionality to
//         automatically restore CVars to what they were pre-test for you.
//
//         So instead of rolling that twice, this lets TestPair handle it.
public abstract partial class GameTest
{
    [SidedDependency(Side.Server)] private readonly IConfigurationManager _serverCfg = default!;
    [SidedDependency(Side.Client)] private readonly IConfigurationManager _clientCfg = default!;

    private readonly Dictionary<string, object> _clientCVarOverrides = new();
    private readonly Dictionary<string, object> _serverCVarOverrides = new();

    /// <summary>
    ///     Adds a setup-time override for a given cvar, for use by <see cref="IGameTestModifier"/>s.
    /// </summary>
    public void PreTestAddOverride(Side side, string cVar, object value)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        if (_setupDone)
            throw new NotSupportedException("Cannot use PreTest functions after test SetUp.");

        if (side is Side.Neither)
            throw new NotSupportedException($"Must specify a side, or both, for {nameof(PreTestAddOverride)}");

        if ((side & Side.Server) != 0)
            _serverCVarOverrides.Add(cVar, value);

        if ((side & Side.Client) != 0)
            _clientCVarOverrides.Add(cVar, value);
    }

    private async Task DoPreTestOverrides()
    {
        foreach (var (cvar, value) in _clientCVarOverrides)
        {
            await OverrideCVarByName(Side.Client, cvar, value, false);
        }

        foreach (var (cvar, value) in _serverCVarOverrides)
        {
            await OverrideCVarByName(Side.Server, cvar, value, false);
        }

        await Pair.RunUntilSynced();
    }

    /// <summary>
    ///     Sets a given CVar for the provided side.
    /// </summary>
    /// <remarks>Does its own cleanup, you do not need to set the CVar back yourself.</remarks>
    public async Task OverrideCVar<T>(Side side, CVarDef<T> cvar, T value, bool sync = true)
        where T: notnull
    {
        await OverrideCVarByName(side, cvar.Name, value, sync);
    }

    private async Task OverrideCVarByName(Side side, string cVar, object value, bool sync)
    {
        if (side is Side.Client)
        {
            _clientCfg.SetCVar(cVar, value);
        }
        else if (side is Side.Server)
        {
            _serverCfg.SetCVar(cVar, value);
        }
        else
        {
            throw new NotSupportedException($"Expected a specific side, got {side}.");
        }

        if (sync)
            await Pair.RunUntilSynced();
    }
}
