using Content.Server._00OuterRim.Worldgen.Systems.Overworld;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server._00OuterRim.Worldgen.Commands;

[AdminCommand(AdminFlags.Debug)]
public class OverworldDebugCommand : IConsoleCommand
{
    public string Command => "debugoverworld";

    public string Description => "a";

    public string Help => "just run it";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is null)
            return;

        EntitySystem.Get<WorldChunkSystem>().OpenEui((IPlayerSession)shell.Player);
    }
}
