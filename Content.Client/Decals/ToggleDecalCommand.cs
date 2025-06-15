using Robust.Shared.Console;

namespace Content.Client.Decals;

public sealed class ToggleDecalCommand : LocalizedCommands
{
    public override string Command => "toggledecals";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
    }
}
