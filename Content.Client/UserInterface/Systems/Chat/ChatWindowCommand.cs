using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Console;
using System.Linq;

namespace Content.Client.UserInterface.Systems.Chat;

/// <summary>
/// Command which creates a window containing a chatbox
/// </summary>
[UsedImplicitly]
public sealed class ChatPanelCommand : LocalizedCommands
{
    public override string Command => "chatpanel";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var window = new ChatWindow();
        window.OpenCentered();
    }
}

[UsedImplicitly]
public sealed class ChatWindowCommand : LocalizedCommands
{
    public override string Command => "chatwindow";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var clyde = IoCManager.Resolve<IClyde>();
        var monitor = clyde.EnumerateMonitors().First();
        if (args.Length > 0)
        {
            var id = int.Parse(args[0]);
            monitor = clyde.EnumerateMonitors().Single(m => m.Id == id);
        }

        var window = clyde.CreateWindow(new WindowCreateParameters
        {
            //Maximized = true,
            Title = Loc.GetString("chat-window-title"),
            //Monitor = monitor,
        });
        var root = IoCManager.Resolve<IUserInterfaceManager>().CreateWindowRoot(window);
        window.DisposeOnClose = true;

        var control = new ChatWindow();

        control.OnClose += () => window.Dispose();

        root.AddChild(control);
    }
}

/// <summary>
/// Command which creates a window containing a chatbox configured for admin use
/// </summary>
[UsedImplicitly]
public sealed class AdminChatWindowCommand : LocalizedCommands
{
    public override string Command => "achatwindow";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var window = new ChatWindow();
        window.ConfigureForAdminChat();
        window.OpenCentered();
    }
}
