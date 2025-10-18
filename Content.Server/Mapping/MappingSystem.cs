using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
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
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly IResourceManager _resMan = default!;
    [Dependency] private readonly MapLoaderSystem _loader = default!;

    // Not a comp because I don't want to deal with this getting saved onto maps ever
    /// <summary>
    ///     map id -> next autosave timespan & original filename.
    /// </summary>
    /// <returns></returns>
    private Dictionary<EntityUid, (TimeSpan next, ResPath fileName)> _currentlyAutosaving = new();

    private bool _autosaveEnabled;
    private ResPath? _path;

    public override void Initialize()
    {
        base.Initialize();

        _conHost.RegisterCommand("toggleautosave",
            "Toggles autosaving for a map.",
            "autosave <map> <path if enabling>",
            ToggleAutosaveCommand);

        Subs.CVar(_cfg, CCVars.AutosaveEnabled, SetAutosaveEnabled, true);
        Subs.CVar(_cfg, CCVars.AutosaveDirectory, SetAutoSavePath, true);
    }

    private void SetAutoSavePath(string value)
    {
        if (!ResPath.IsValidPath(value))
        {
            Log.Error($"Invalid path: {value}");
            _path = null;
            return;
        }

        _path = new ResPath(value).ToRootedPath();
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

        if (!_autosaveEnabled || _path == null)
            return;

        foreach (var (uid, (time, name))in _currentlyAutosaving)
        {
            if (_timing.RealTime <= time)
                continue;

            if (LifeStage(uid) >= EntityLifeStage.MapInitialized)
            {
                Log.Warning($"Can't autosave entity {uid}; it doesn't exist, or is initialized. Removing from autosave.");
                _currentlyAutosaving.Remove(uid);
                continue;
            }

            _currentlyAutosaving[uid] = (CalculateNextTime(), name);
            var saveDir = _path.Value / name;
            _resMan.UserData.CreateDir(saveDir);

            var path = saveDir / new ResPath($"{DateTime.Now:yyyy-M-dd_HH.mm.ss}-AUTO.yml");
            Log.Info($"Autosaving map {name} ({uid}) to {path}. Next save in {ReadableTimeLeft(uid)} seconds.");

            if (HasComp<MapComponent>(uid))
                _loader.TrySaveMap(uid, path);
            else
                _loader.TrySaveGrid(uid, path);
        }
    }

    private TimeSpan CalculateNextTime()
    {
        return _timing.RealTime + TimeSpan.FromSeconds(_cfg.GetCVar(CCVars.AutosaveInterval));
    }

    private double ReadableTimeLeft(EntityUid uid)
    {
        return Math.Round(_currentlyAutosaving[uid].next.TotalSeconds - _timing.RealTime.TotalSeconds);
    }

    #region Public API

    public void ToggleAutosave(MapId map, string? name = null)
    {
        if (_map.TryGetMap(map, out var uid))
            ToggleAutosave(uid.Value, name);
    }

    public void ToggleAutosave(EntityUid uid, string? name = null)
    {
        if (!_autosaveEnabled)
            return;

        if (_currentlyAutosaving.Remove(uid) || name == null)
            return;

        if (LifeStage(uid) >= EntityLifeStage.MapInitialized)
        {
            Log.Error("Tried to enable autosaving on a post map-init entity.");
            return;
        }

        if (!HasComp<MapComponent>(uid) && !HasComp<MapGridComponent>(uid))
        {
            Log.Error($"{ToPrettyString(uid)} is neither a grid or map");
            return;
        }

        if (!ResPath.IsValidFilename(name))
        {
            Log.Error($"Not a valid filename: {name}");
            return;
        }

        _currentlyAutosaving[uid] = (CalculateNextTime(), new ResPath(name));
        Log.Info($"Started autosaving map {name} ({uid}). Next save in {ReadableTimeLeft(uid)} seconds.");
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
