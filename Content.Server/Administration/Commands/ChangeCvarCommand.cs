using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using Content.Shared.Database;
using Robust.Shared.Configuration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

/// <summary>
/// Allows admins to change certain CVars. This is different than the "cvar" command which is host only and can change any CVar.
/// </summary>
/// <remarks>
/// Possible todo for future, store default values for cvars, and allow resetting to default.
/// </remarks>
[AnyCommand]
public sealed class ChangeCvarCommand : IConsoleCommand
{
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly IAdminLogManager _adminLogManager = default!;
    [Dependency] private readonly CVarControlManager _cVarControlManager = default!;

    /// <summary>
    /// Searches the list of cvars for a cvar that matches the search string.
    /// </summary>
    private void SearchCVars(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 2)
        {
            shell.WriteLine(Loc.GetString("cmd-changecvar-search-no-arguments"));
            return;
        }

        var cvars = _cVarControlManager.GetAllRunnableCvars(shell);

        var matches = cvars
            .Where(c =>
                c.Name.Contains(args[1], StringComparison.OrdinalIgnoreCase)
                || c.ShortHelp?.Contains(args[1], StringComparison.OrdinalIgnoreCase) == true
                || c.LongHelp?.Contains(args[1], StringComparison.OrdinalIgnoreCase) == true
                ) // Might be very slow and stupid, but eh.
            .ToList();

        if (matches.Count == 0)
        {
            shell.WriteLine(Loc.GetString("cmd-changecvar-search-no-matches"));
            return;
        }

        shell.WriteLine(Loc.GetString("cmd-changecvar-search-matches", ("count", matches.Count)));
        shell.WriteLine(string.Join("\n", matches.Select(FormatCVarFullHelp)));
    }

    /// <summary>
    /// Formats a CVar into a string for display.
    /// </summary>
    private string FormatCVarFullHelp(ChangableCVar cvar)
    {
        if (cvar.LongHelp != null && cvar.ShortHelp != null)
        {
            return $"{cvar.Name} - {cvar.LongHelp}";
        }

        // There is no help, no one is coming. We are all doomed.
        return cvar.Name;
    }

    public string Command => "changecvar";
    public string Description { get; } = Loc.GetString("cmd-changecvar-desc");
    public string Help { get; } = Loc.GetString("cmd-changecvar-help");
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            shell.WriteLine(Loc.GetString("cmd-changecvar-no-arguments"));
            return;
        }

        var cvars = _cVarControlManager.GetAllRunnableCvars(shell);

        var cvar = args[0];
        if (cvar == "?")
        {
            if (cvars.Count == 0)
            {
                shell.WriteLine(Loc.GetString("cmd-changecvar-no-cvars"));
                return;
            }

            shell.WriteLine(Loc.GetString("cmd-changecvar-available-cvars"));
            shell.WriteLine(string.Join("\n", cvars.Select(FormatCVarFullHelp)));
            return;
        }

        if (cvar == "search")
        {
            SearchCVars(shell, argStr, args);
            return;
        }

        if (!_configurationManager.IsCVarRegistered(cvar)) // Might be a redunat check with the if statement below.
        {
            shell.WriteLine(Loc.GetString("cmd-changecvar-cvar-not-registered", ("cvar", cvar)));
            return;
        }

        if (cvars.All(c => c.Name != cvar))
        {
            shell.WriteLine(Loc.GetString("cmd-changecvar-cvar-not-allowed"));
            return;
        }

        if (args.Length == 1)
        {
            var value = _configurationManager.GetCVar<object>(cvar);
            shell.WriteLine(value.ToString()!);
        }
        else
        {
            var value = args[1];
            var type = _configurationManager.GetCVarType(cvar);
            try
            {
                var parsed = CVarCommandUtil.ParseObject(type, value);
                // Value check, is it in the min/max range?
                var control = _cVarControlManager.GetCVar(cvar)!.Control; // Null check is done above.
                var allowed = true;
                if (control is { Min: not null, Max: not null })
                {
                    switch (parsed) // This looks bad, and im not sorry.
                    {
                        case int intVal:
                        {
                            if (intVal < (int)control.Min || intVal > (int)control.Max)
                            {
                                allowed = false;
                            }

                            break;
                        }
                        case float floatVal:
                        {
                            if (floatVal < (float)control.Min || floatVal > (float)control.Max)
                            {
                                allowed = false;
                            }

                            break;
                        }
                        case long longVal:
                        {
                            if (longVal < (long)control.Min || longVal > (long)control.Max)
                            {
                                allowed = false;
                            }

                            break;
                        }
                        case ushort ushortVal:
                        {
                            if (ushortVal < (ushort)control.Min || ushortVal > (ushort)control.Max)
                            {
                                allowed = false;
                            }

                            break;
                        }
                    }
                }

                if (!allowed)
                {
                    shell.WriteError(Loc.GetString("cmd-changecvar-value-out-of-range",
                        ("min", control.Min ?? "-∞"),
                        ("max", control.Max ?? "∞")));
                    return;
                }

                var oldValue = _configurationManager.GetCVar<object>(cvar);
                _configurationManager.SetCVar(cvar, parsed);
                _adminLogManager.Add(LogType.AdminCommands,
                    LogImpact.Extreme,
                    $"{shell.Player!.Name} ({shell.Player!.UserId}) changed CVAR {cvar} from {oldValue.ToString()} to {parsed.ToString()}"
                    );

                shell.WriteLine(Loc.GetString("cmd-changecvar-success", ("cvar", cvar), ("old", oldValue), ("value", parsed)));
            }
            catch (FormatException)
            {
                shell.WriteError(Loc.GetString("cmd-cvar-parse-error", ("type", type)));
            }
        }
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        var cvars = _cVarControlManager.GetAllRunnableCvars(shell);

        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                cvars
                    .Select(c => new CompletionOption(c.Name, c.ShortHelp ?? c.Name)),
                Loc.GetString("cmd-changecvar-arg-name"));
        }

        var cvar = args[0];
        if (!_configurationManager.IsCVarRegistered(cvar))
            return CompletionResult.Empty;

        var type = _configurationManager.GetCVarType(cvar);
        return CompletionResult.FromHint($"<{type.Name}>");
    }
}
