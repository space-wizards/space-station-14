using Content.Client.NPC.HTN;
using Robust.Shared.Console;

namespace Content.Client.NPC;

public sealed class ShowHTNCommand : IConsoleCommand
{
    public string Command => "showhtn";
    public string Description => "Shows the current status for HTN NPCs";
    public string Help => $"{Command}";
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var npcs = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<HTNSystem>();
        npcs.EnableOverlay ^= true;
    }
}
