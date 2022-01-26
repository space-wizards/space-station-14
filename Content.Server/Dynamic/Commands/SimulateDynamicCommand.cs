using Content.Server.Administration;
using Content.Server.Dynamic.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;

namespace Content.Server.Dynamic.Commands;

/// <summary>
///     Reruns the dynamic budget/threat calculations.
/// </summary>
[AdminCommand(AdminFlags.Admin)]
public class SimulateDynamicCommand : IConsoleCommand
{
    public string Command => "simulatedynamic";
    public string Description => "a";
    public string Help => "a";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var dynamic = EntitySystem.Get<DynamicModeSystem>();
        dynamic.GenerateThreat();
        dynamic.GenerateBudgets();
    }
}
