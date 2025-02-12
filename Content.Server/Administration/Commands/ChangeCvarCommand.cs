using System.Linq;
using System.Reflection;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using Content.Shared.CCVar.CVarAccess;
using Content.Shared.Database;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Reflection;

namespace Content.Server.Administration.Commands;

/// <summary>
/// Allows admins to change certain CVars. This is different than the "cvar" command which is host only and can change any CVar.
/// </summary>
/// <remarks>
/// Possible todo for future, store default values for cvars, and allow resetting to default.
/// </remarks>
[AdminCommand(AdminFlags.Admin)]
public sealed class ChangeCvarCommand : LocalizedCommands
{
    [Dependency] private readonly IReflectionManager _reflectionManager = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly IAdminLogManager _adminLogManager = default!;

    private List<ChangableCVar> _changableCvars = new();
    private bool _initialized;

    private void Init()
    {
        if (_initialized) // hate this.
            return;

        _initialized = true;

        var validCvarsDefs = _reflectionManager.FindTypesWithAttribute<CVarDefsAttribute>();

        foreach (var type in validCvarsDefs)
        {
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
            {
                var allowed = field.GetCustomAttribute<CVarControl>();
                if (allowed == null)
                {
                    continue;
                }

                var cvarDef = (CVarDef)field.GetValue(null)!;
                _changableCvars.Add(new ChangableCVar(cvarDef.Name, allowed));
                // Possible todo? check if the cvar is registered in the config manager? or already registered in our list?
                // Think engine will blow up anyways if its double registered? Not sure.
                // This command should be fine with multiple registrations.
            }
        }
    }

    public override string Command => "changecvar";
    public override string Description { get; } = Loc.GetString("cmd-changecvar-desc");
    public override string Help { get; } = Loc.GetString("cmd-changecvar-help");
    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            shell.WriteLine(Loc.GetString("cmd-changecvar-no-arguments"));
            return;
        }

        var cvars = GetAllRunnableCvars(shell);

        var cvar = args[0];
        if (cvar == "?")
        {
            if (cvars.Count == 0)
            {
                shell.WriteLine(Loc.GetString("cmd-changecvar-no-cvars"));
                return;
            }

            shell.WriteLine(Loc.GetString("cmd-changecvar-available-cvars"));
            shell.WriteLine(string.Join("\n", cvars.Select(c => c.Name)));
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
                var control = _changableCvars.First(c => c.Name == cvar).Control;
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
                    LogImpact.High,
                    $"{shell.Player!.Name} ({shell.Player!.UserId}) changed CVAR {cvar} from {oldValue.ToString()} to {parsed.ToString()}"
                    );
            }
            catch (FormatException)
            {
                shell.WriteError(Loc.GetString("cmd-cvar-parse-error", ("type", type)));
            }
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        // This is kinda scrunkly.
        // Ideally I would run init the moment the dependencies are injected, but IPostInjectInit.PostInject()
        // does not run on commands. Will result in first completion being a bit slow, but eh, it's fine.
        Init();

        var cvars = GetAllRunnableCvars(shell);

        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                cvars.Select(c => c.Name)
                    .Select(c => new CompletionOption(c)),
                Loc.GetString("cmd-cvar-arg-name"));
        }

        var cvar = args[0];
        if (!_configurationManager.IsCVarRegistered(cvar))
            return CompletionResult.Empty;

        var type = _configurationManager.GetCVarType(cvar);
        return CompletionResult.FromHint($"<{type.Name}>");
    }

    private List<ChangableCVar> GetAllRunnableCvars(IConsoleShell shell)
    {
        // Not a player, running as server. We COULD return all cvars,
        // but a check later down the line will prevent it from anyways. Use the "cvar" command instead.
        if (shell.Player == null)
            return [];

        var adminData = _adminManager.GetAdminData(shell.Player);
        if (adminData == null)
            return []; // Not an admin

        return _changableCvars
            .Where(cvar => adminData.HasFlag(cvar.Control.AdminFlags))
            .ToList();
    }

    private sealed class ChangableCVar
    {
        public string Name { get; }

        // Holding a reference to the attribute might be skrunkly? Not sure how much mem it eats up.
        public CVarControl Control { get; }

        public ChangableCVar(string name, CVarControl control)
        {
            Name = name;
            Control = control;
        }
    }
}
