using Content.Client.Credits;
using Content.Shared.Administration;
using JetBrains.Annotations;
using Robust.Shared.Console;

namespace Content.Client.Commands;

[UsedImplicitly, AnyCommand]
public sealed class CreditsCommand : LocalizedCommands
{
    public override string Command => "credits";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        new CreditsWindow().Open();
    }
}
