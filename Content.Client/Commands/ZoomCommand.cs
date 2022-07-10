using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Shared.Console;

namespace Content.Client.Commands;

[UsedImplicitly]
public sealed class ZoomCommand : IConsoleCommand
{
    [Dependency] private readonly IEyeManager _eyeMan = default!;

    public string Command => "zoom";
    public string Description => Loc.GetString("zoom-command-description");
    public string Help => Loc.GetString("zoom-command-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        Vector2 zoom;
        if (args.Length is not (1 or 2))
        {
            shell.WriteLine(Help);
            return;
        }

        if (!float.TryParse(args[0], out var arg0))
        {
            shell.WriteError(Loc.GetString("cmd-parse-failure-float", ("arg", args[0])));
            return;
        }

        if (arg0 > 0)
            zoom = new(arg0, arg0);
        else
        {
            shell.WriteError(Loc.GetString("zoom-command-error"));
            return;
        }

        if (args.Length == 2)
        {
            if (!float.TryParse(args[1], out var arg1))
            {
                shell.WriteError(Loc.GetString("cmd-parse-failure-float", ("arg", args[1])));
                return;
            }

            if (arg1 > 0)
                zoom.Y = arg1;
            else
            {
                shell.WriteError(Loc.GetString("zoom-command-error"));
                return;
            }
        }

        _eyeMan.CurrentEye.Zoom = zoom;
    }
}
