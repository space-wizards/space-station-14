#nullable enable
using System.Collections.Generic;
using Robust.Shared.Configuration;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Pair;

public sealed partial class TestPair
{
    private readonly Dictionary<string, object> _modifiedClientCvars = new();
    private readonly Dictionary<string, object> _modifiedServerCvars = new();

    private void OnServerCvarChanged(IConfigurationManager.CvarChangeArgs args)
    {
        _modifiedServerCvars.TryAdd(args.Name, args.OldValue);
    }

    private void OnClientCvarChanged(IConfigurationManager.CvarChangeArgs args)
    {
        _modifiedClientCvars.TryAdd(args.Name, args.OldValue);
    }

    internal void ClearModifiedCvars()
    {
        _modifiedClientCvars.Clear();
        _modifiedServerCvars.Clear();
    }

    /// <summary>
    /// Reverts any cvars that were modified during a test back to their original values.
    /// </summary>
    public async Task RevertModifiedCvars()
    {
        await Server.WaitPost(() =>
        {
            foreach (var (name, value) in _modifiedServerCvars)
            {
                Server.Log.Info($"Resetting cvar {name} to {value}");
                Server.CfgMan.SetCVar(name, value);
            }
        });

        await Client.WaitPost(() =>
        {
            foreach (var (name, value) in _modifiedClientCvars)
            {
                var flags = Client.CfgMan.GetCVarFlags(name);
                if (flags.HasFlag(CVar.REPLICATED) && flags.HasFlag(CVar.SERVER))
                    return;

                Client.Log.Info($"Resetting cvar {name} to {value}");
                Client.CfgMan.SetCVar(name, value);
            }
        });

        ClearModifiedCvars();
    }
}
