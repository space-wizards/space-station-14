using System.IO;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Mapping;

/// <summary>
///     Handles autosaving maps.
/// </summary>
public sealed class MappingSystem : EntitySystem
{
    [Dependency] private readonly IConsoleHost _conHost = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IResourceManager _resMan = default!;
    [Dependency] private readonly MapLoaderSystem _map = default!;

    // Not a comp because I don't want to deal with this getting saved onto maps ever
    /// <summary>
    ///     map id -> next autosave timespan & original filename.
    /// </summary>
    /// <returns></returns>
    private Dictionary<MapId, (TimeSpan next, string fileName)> _currentlyAutosaving = new();

    private bool _autosaveEnabled;

    public override void Initialize()
    {
        base.Initialize();

        _conHost.RegisterCommand("toggleautosave",
            "Toggles autosaving for a map.",
            "autosave <map> <path if enabling>",
            ToggleAutosaveCommand);

        Subs.CVar(_cfg, CCVars.AutosaveEnabled, SetAutosaveEnabled, true);
    }

    private void SetAutosaveEnabled(bool b)
    {
        if (!b)
            _currentlyAutosaving.Clear();
        _autosaveEnabled = b;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_autosaveEnabled)
            return;

        foreach (var (map, (time, name))in _currentlyAutosaving.ToArray())
        {
            if (_timing.RealTime <= time)
                continue;

            if (!_mapManager.MapExists(map) || _mapManager.IsMapInitialized(map))
            {
                Log.Warning($"Can't autosave map {map}; it doesn't exist, or is initialized. Removing from autosave.");
                _currentlyAutosaving.Remove(map);
                return;
            }

            var saveDir = Path.Combine(_cfg.GetCVar(CCVars.AutosaveDirectory), name);
            _resMan.UserData.CreateDir(new ResPath(saveDir).ToRootedPath());

            var path = Path.Combine(saveDir, $"{DateTime.Now.ToString("yyyy-M-dd_HH.mm.ss")}-AUTO.yml");
            _currentlyAutosaving[map] = (CalculateNextTime(), name);
            Log.Info($"Autosaving map {name} ({map}) to {path}. Next save in {ReadableTimeLeft(map)} seconds.");
            _map.SaveMap(map, path);
        }
    }

    private TimeSpan CalculateNextTime()
    {
        return _timing.RealTime + TimeSpan.FromSeconds(_cfg.GetCVar(CCVars.AutosaveInterval));
    }

    private double ReadableTimeLeft(MapId map)
    {
        return Math.Round(_currentlyAutosaving[map].next.TotalSeconds - _timing.RealTime.TotalSeconds);
    }

    #region Public API

    public void ToggleAutosave(MapId map, string? path=null)
    {
        if (!_autosaveEnabled)
            return;

        if (path != null && _currentlyAutosaving.TryAdd(map, (CalculateNextTime(), Path.GetFileName(path))))
        {
            if (!_mapManager.MapExists(map) || _mapManager.IsMapInitialized(map))
            {
                Log.Warning("Tried to enable autosaving on non-existant or already initialized map!");
                _currentlyAutosaving.Remove(map);
                return;
            }

            Log.Info($"Started autosaving map {path} ({map}). Next save in {ReadableTimeLeft(map)} seconds.");
        }
        else
        {
            _currentlyAutosaving.Remove(map);
            Log.Info($"Stopped autosaving on map {map}");
        }
    }

    #endregion

    #region Commands

    [AdminCommand(AdminFlags.Server | AdminFlags.Mapping)]
    private void ToggleAutosaveCommand(IConsoleShell shell, string argstr, string[] args)
    {
        if (args.Length != 1 && args.Length != 2)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!int.TryParse(args[0], out var intMapId))
        {
            shell.WriteError(Loc.GetString("cmd-mapping-failure-integer", ("arg", args[0])));
            return;
        }

        string? path = null;
        if (args.Length == 2)
        {
            path = args[1];
        }

        var mapId = new MapId(intMapId);
        ToggleAutosave(mapId, path);
    }

    #endregion
}
