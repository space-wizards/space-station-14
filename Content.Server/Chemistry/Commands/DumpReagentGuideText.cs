using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.Commands;

[AdminCommand(AdminFlags.Debug)]
public sealed class DumpReagentGuideText : IConsoleCommand
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IEntitySystemManager _entSys = default!;

    public string Command => "dumpreagentguidetext";
    public string Description => "Dumps the guidebook text for a reagent to the console";
    public string Help => "dumpreagentguidetext <reagent>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError("Must have only 1 argument");
            return;
        }

        if (!_prototype.TryIndex<ReagentPrototype>(args[0], out var reagent))
        {
            shell.WriteError($"Invalid prototype: {args[0]}");
            return;
        }

        if (reagent.Metabolisms is null)
        {
            shell.WriteLine("Nothing to dump.");
            return;
        }

        foreach (var entry in reagent.Metabolisms.Values)
        {
            foreach (var effect in entry.Effects)
            {
                shell.WriteLine(effect.GuidebookEffectDescription(_prototype, _entSys) ?? $"[skipped effect of type {effect.GetType()}]");
            }
        }
    }
}
