using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Motd;

/// <summary>
/// A command that can be used by any player to print the Message of the Day.
/// </summary>
[AnyCommand]
public sealed class GetMotdCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public string Command => "get-motd";
    public string Description => "Prints the Message Of The Day.";
    public string Help => "get-motd";
    
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        _entityManager.EntitySysManager.GetEntitySystem<MOTDSystem>().TrySendMOTD(shell);
    }
}
