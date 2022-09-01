using System.Globalization;
using System.IO;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Microsoft.Extensions.Configuration;
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
    [Dependency] private readonly IMapLoader _mapLoader = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IResourceManager _resMan = default!;

    // Not a comp because I don't want to deal with this getting saved onto maps ever
    /// <summary>
    ///     map id -> next autosave timespan.
    /// </summary>
    /// <returns></returns>
    private Dictionary<MapId, TimeSpan> _currentlyAutosaving = new();

    private ISawmill _sawmill = default!;
    private bool _autosaveEnabled;

    public override void Initialize()
    {
        base.Initialize();

        _conHost.RegisterCommand("toggleautosave",
            "Toggles autosaving for a map.",
            "autosave <map>",
            ToggleAutosaveCommand);

        _sawmill = Logger.GetSawmill("autosave");
        _cfg.OnValueChanged(CCVars.AutosaveEnabled, SetAutosaveEnabled, true);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _cfg.UnsubValueChanged(CCVars.AutosaveEnabled, SetAutosaveEnabled);
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

        foreach (var (map, time) in _currentlyAutosaving.ToArray())
        {
            if (_timing.RealTime <= time)
                continue;

            if (!_mapManager.MapExists(map) || _mapManager.IsMapInitialized(map))
            {
                _sawmill.Warning($"Can't autosave map {map}; it doesn't exist, or is initialized. Removing from autosave.");
                _currentlyAutosaving.Remove(map);
                return;
            }

            var autosavesDir = _cfg.GetCVar(CCVars.AutosaveDirectory);
            var dir = new ResourcePath(autosavesDir).ToRootedPath();
            _resMan.UserData.CreateDir(dir);

            var path = Path.Combine(autosavesDir, $"{DateTime.Now.ToString("yyyy-M-dd_HH.mm.ss")}-AUTO.yml");
            _currentlyAutosaving[map] = CalculateNextTime();
            _sawmill.Info($"Autosaving map {map} to {path}. Next save in {ReadableTimeLeft(map)} seconds.");
            _mapLoader.SaveMap(map, path);
        }
    }

    private TimeSpan CalculateNextTime()
    {
        return _timing.RealTime + TimeSpan.FromSeconds(_cfg.GetCVar(CCVars.AutosaveInterval));
    }

    private double ReadableTimeLeft(MapId map)
    {
        return Math.Round(_currentlyAutosaving[map].TotalSeconds - _timing.RealTime.TotalSeconds);
    }

    #region Public API

    public void ToggleAutosave(MapId map)
    {
        if (!_autosaveEnabled)
            return;

        if (_currentlyAutosaving.TryAdd(map, CalculateNextTime()))
        {
            if (!_mapManager.MapExists(map) || _mapManager.IsMapInitialized(map))
            {
                _sawmill.Warning("Tried to enable autosaving on non-existant or already initialized map!");
                _currentlyAutosaving.Remove(map);
                return;
            }

            _sawmill.Info($"Started autosaving map {map}. Next save in {ReadableTimeLeft(map)} seconds.");
        }
        else
        {
            _currentlyAutosaving.Remove(map);
            _sawmill.Info($"Stopped autosaving on map {map}");
        }
    }

    #endregion

    #region Commands

    [AdminCommand(AdminFlags.Server | AdminFlags.Mapping)]
    private void ToggleAutosaveCommand(IConsoleShell shell, string argstr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!int.TryParse(args[0], out var intMapId))
        {
            shell.WriteError(Loc.GetString("cmd-mapping-failure-integer", ("arg", args[0])));
            return;
        }

        var mapId = new MapId(intMapId);
        ToggleAutosave(mapId);
    }

    #endregion
}
