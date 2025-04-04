using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Console;

namespace Content.Server.Movement.Commands;

/// <summary>
/// Temporary command to enable admins to toggle the mob collision cvar.
/// </summary>
[AdminCommand(AdminFlags.VarEdit)]
public sealed class ToggleMobCollisionCommand : IConsoleCommand
{
    [Dependency] private readonly IConfigurationManager _cfgManager = default!;

    public string Command => "toggle_mob_collision";
    public string Description => "Toggles mob collision";
    public string Help => Description;
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        _cfgManager.SetCVar(CCVars.MovementMobPushing, !_cfgManager.GetCVar(CCVars.MovementMobPushing));
    }
}
