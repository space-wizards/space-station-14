using System.IO;
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
    private Dictionary<EntityUid, (TimeSpan next, string fileName)> _currentlyAutosaving = new();

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
            var saveDir = Path.Combine(_cfg.GetCVar(CCVars.AutosaveDirectory), name);
            _resMan.UserData.CreateDir(new ResPath(saveDir).ToRootedPath());

            var path = new ResPath(Path.Combine(saveDir, $"{DateTime.Now:yyyy-M-dd_HH.mm.ss}-AUTO.yml"));
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

    public void ToggleAutosave(MapId map, string? path = null)
    {
        if (_map.TryGetMap(map, out var uid))
            ToggleAutosave(uid.Value, path);
    }

    public void ToggleAutosave(EntityUid uid, string? path=null)
    {
        if (!_autosaveEnabled)
            return;

        if (_currentlyAutosaving.Remove(uid) || path == null)
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

        _currentlyAutosaving[uid] = (CalculateNextTime(), Path.GetFileName(path));
        Log.Info($"Started autosaving map {path} ({uid}). Next save in {ReadableTimeLeft(uid)} seconds.");
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
