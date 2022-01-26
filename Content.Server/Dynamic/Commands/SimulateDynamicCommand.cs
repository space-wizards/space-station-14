using System.Linq;
using Content.Server.Administration;
using Content.Server.Dynamic.Systems;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

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
        var players = IoCManager.Resolve<IPlayerManager>().ServerSessions.ToArray();
        dynamic.GenerateThreat();
        dynamic.GenerateBudgets();
        dynamic.RunRoundstartEvents(players);
    }
}
