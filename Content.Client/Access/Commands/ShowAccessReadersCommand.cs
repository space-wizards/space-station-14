using Robust.Shared.Console;

namespace Content.Client.Access.Commands;

public sealed class ShowAccessReadersCommand : IConsoleCommand
{
    public string Command => "showaccessreaders";
    public string Description => "Shows all access readers in the viewport";
    public string Help => $"{Command}";
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var access = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<AccessSystem>();

        access.ReaderOverlay ^= true;
        shell.WriteLine($"Set access reader debug overlay to {access.ReaderOverlay}");
    }
}
