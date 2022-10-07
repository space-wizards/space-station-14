using System.IO;
using Content.Shared.Administration;
using Robust.Client.UserInterface;
using Robust.Shared.Console;

namespace Content.Client.Administration.Commands;

public sealed class LoadPrototypeCommand : IConsoleCommand
{
    public string Command { get; } = "loadprototype";
    public string Description { get; } = "Load a prototype file into the server.";
    public string Help => Command;

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        LoadPrototype();
    }

    public static async void LoadPrototype()
    {
        var dialogManager = IoCManager.Resolve<IFileDialogManager>();
        var loadManager = IoCManager.Resolve<IGamePrototypeLoadManager>();

        var stream = await dialogManager.OpenFile();
        if (stream is null)
            return;

        // ew oop
        var reader = new StreamReader(stream);
        var proto = await reader.ReadToEndAsync();
        loadManager.SendGamePrototype(proto);
    }
}
