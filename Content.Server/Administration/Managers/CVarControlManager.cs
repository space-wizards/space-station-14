using System.Linq;
using System.Reflection;
using Content.Shared.CCVar.CVarAccess;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Player;
using Robust.Shared.Reflection;

namespace Content.Server.Administration.Managers;

/// <summary>
/// Manages the control of CVars via the <see cref="Content.Shared.CCVar.CVarAccess.CVarControl"/> attribute.
/// </summary>
public sealed class CVarControlManager : IPostInjectInit
{
    [Dependency] private readonly IReflectionManager _reflectionManager = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly ILocalizationManager _localizationManager = default!;
    [Dependency] private readonly ILogManager _logger = default!;

    private readonly List<ChangableCVar> _changableCvars = new();
    private ISawmill _sawmill = default!;

    void IPostInjectInit.PostInject()
    {
        _sawmill = _logger.GetSawmill("cvarcontrol");
    }

    public void Initialize()
    {
        RegisterCVars();
    }

    private void RegisterCVars()
    {
        if (_changableCvars.Count != 0)
        {
            _sawmill.Warning("CVars already registered, overwriting.");
            _changableCvars.Clear();
        }

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
                _changableCvars.Add(new ChangableCVar(cvarDef.Name, allowed, _localizationManager));
            }
        }

        _sawmill.Info($"Registered {_changableCvars.Count} CVars.");
    }

    /// <summary>
    /// Gets all CVars that the player can change.
    /// </summary>
    public List<ChangableCVar> GetAllRunnableCvars(IConsoleShell shell)
    {
        // Not a player, running as server. We COULD return all cvars,
        // but a check later down the line will prevent it from anyways. Use the "cvar" command instead.
        if (shell.Player == null)
            return [];

        return GetAllRunnableCvars(shell.Player);
    }

    public List<ChangableCVar> GetAllRunnableCvars(ICommonSession session)
    {
        var adminData = _adminManager.GetAdminData(session);
        if (adminData == null)
            return []; // Not an admin

        return _changableCvars
            .Where(cvar => adminData.HasFlag(cvar.Control.AdminFlags))
            .ToList();
    }

    public ChangableCVar? GetCVar(string name)
    {
        return _changableCvars.FirstOrDefault(cvar => cvar.Name == name);
    }
}

public sealed class ChangableCVar
{
    private const string LocPrefix = "changecvar";

    public string Name { get; }

    // Holding a reference to the attribute might be skrunkly? Not sure how much mem it eats up.
    public CVarControl Control { get; }

    public string? ShortHelp;
    public string? LongHelp;

    public ChangableCVar(string name, CVarControl control, ILocalizationManager loc)
    {
        Name = name;
        Control = control;

        if (loc.TryGetString($"{LocPrefix}-simple-{name.Replace('.', '_')}", out var simple))
        {
            ShortHelp = simple;
        }

        if (loc.TryGetString($"{LocPrefix}-full-{name.Replace('.', '_')}", out var longHelp))
        {
            LongHelp = longHelp;
        }

        // If one is set and the other is not, we throw
        if (ShortHelp == null && LongHelp != null || ShortHelp != null && LongHelp == null)
        {
            throw new InvalidOperationException("Short and long help must both be set or both be null.");
        }
    }
}
