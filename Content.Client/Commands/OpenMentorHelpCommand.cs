using Content.Client.UserInterface.Systems.Bwoink;
using Content.Shared.Administration;
using Robust.Client.UserInterface;
using Robust.Shared.Console;
using Robust.Shared.Network;

namespace Content.Client.Commands;

[AnyCommand]
public sealed class OpenMentorHelpCommand : LocalizedCommands
{
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;

    public override string Command => "openmentorhelp";

    public override string Help => LocalizationManager.GetString($"cmd-{Command}-help", ("command", Command));

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length >= 2)
        {
            shell.WriteLine(Help);
            return;
        }
        if (args.Length == 0)
        {
            _userInterfaceManager.GetUIController<MentorHelpUIController>().Open();
        }
        else
        {
            if (Guid.TryParse(args[0], out var guid))
            {
                var targetUser = new NetUserId(guid);
                _userInterfaceManager.GetUIController<MentorHelpUIController>().Open(targetUser);
            }
            else
            {
                shell.WriteError(LocalizationManager.GetString($"cmd-{Command}-error"));
            }
        }
    }
}
