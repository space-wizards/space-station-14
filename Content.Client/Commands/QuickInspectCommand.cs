using System.Linq;
using Content.Shared.CCVar;
using Content.Shared.Input;
using Robust.Client.Input;
using Robust.Shared.Configuration;
using Robust.Shared.Console;

namespace Content.Client.Commands;

/// <summary>
/// Sets the a <see cref="CCVars.DebugQuickInspect"/> CVar to the name of a component, which allows the client to quickly open a VV window for that component
/// by using the Alt+C or Alt+B hotkeys.
/// </summary>
public sealed class QuickInspectCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;

    public override string Command => "quickinspect";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteLine(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        _configurationManager.SetCVar(CCVars.DebugQuickInspect, args[0]);

        var serverKey = _inputManager.GetKeyFunctionButtonString(ContentKeyFunctions.InspectServerComponent);
        var clientKey = _inputManager.GetKeyFunctionButtonString(ContentKeyFunctions.InspectClientComponent);
        shell.WriteLine(Loc.GetString($"cmd-quickinspect-success", ("component", args[0]), ("serverKeybind", serverKey), ("clientKeybind", clientKey)));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            // Not ideal since it only shows client-side components, but you can still type in any name you want.
            // If you know how to get server component names on the client then please fix this.
            var options = EntityManager.ComponentFactory.AllRegisteredTypes
                .Select(p => new CompletionOption(
                    EntityManager.ComponentFactory.GetComponentName(p)
                ));

            return CompletionResult.FromOptions(options);
        }

        return CompletionResult.Empty;
    }
}
