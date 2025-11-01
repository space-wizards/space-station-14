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

    public void SetAutosaveEnabled(bool b)
    {
        _autosaveEnabled = b;
    }

    public override void Update(float frameTime)
	{
		base.Update(frameTime);

		if (!_autosaveEnabled)
			return;

        // Maps are paused while in mapping, so we have to use AllEntityQuery to get them.
		var query = AllEntityQuery<AutoSaveComponent>();
		while (query.MoveNext(out var uid, out var auto))
		{
			if (_timing.RealTime <= auto.NextSaveTime)
				continue;

            if (LifeStage(uid) >= EntityLifeStage.MapInitialized) // Saving post-init maps or grids has a high chance of throwing errors.
            {
                Log.Warning($"Can't autosave entity {ToPrettyString(uid)}; it is not paused. Removing component.");
                RemCompDeferred<AutoSaveComponent>(uid);
                continue;
            }

			if (!HasComp<MapComponent>(uid) && !HasComp<MapGridComponent>(uid))
			{
				Log.Warning($"Can't autosave entity {ToPrettyString(uid)}; it is not a map or grid. Removing component.");
				RemCompDeferred<AutoSaveComponent>(uid);
				continue;
			}

			auto.NextSaveTime = _timing.RealTime + TimeSpan.FromSeconds(_cfg.GetCVar(CCVars.AutosaveInterval));

			var saveDir = Path.Combine(_cfg.GetCVar(CCVars.AutosaveDirectory), auto.FileName).Replace(Path.DirectorySeparatorChar, '/');
			_resMan.UserData.CreateDir(new ResPath(saveDir).ToRootedPath());

			var path = new ResPath(Path.Combine(saveDir, $"{DateTime.Now:yyyy-M-dd_HH.mm.ss}-AUTO.yml"));
			Log.Info($"Autosaving map {auto.FileName} ({ToPrettyString(uid)}) to {path}. Next save in {ReadableTimeLeft(uid)} seconds.");

			if (HasComp<MapComponent>(uid))
				_loader.TrySaveMap(uid, path);
			else
				_loader.TrySaveGrid(uid, path);
		}
	}

    private double ReadableTimeLeft(EntityUid uid)
    {
		if (!TryComp<AutoSaveComponent>(uid, out var comp))
			return 0;

		return Math.Round(comp.NextSaveTime.TotalSeconds - _timing.RealTime.TotalSeconds);
	}

    #region Public API

    public void ToggleAutosave(MapId map, string? path = null)
    {
        if (_map.TryGetMap(map, out var uid))
            ToggleAutosave(uid.Value, path);
    }

    public void ToggleAutosave(EntityUid uid, string? path = null)
    {
        if (!_autosaveEnabled)
            return;

		if (HasComp<AutoSaveComponent>(uid))
		{
			RemCompDeferred<AutoSaveComponent>(uid);
			return;
		}

		if (string.IsNullOrWhiteSpace(path))
			return;

		if (!HasComp<MapComponent>(uid) && !HasComp<MapGridComponent>(uid))
		{
			Log.Error($"Tried to toggle autosave for {ToPrettyString(uid)}, but it is neither a grid or map!");
			return;
		}

		var comp = EnsureComp<AutoSaveComponent>(uid);
		comp.FileName = Path.GetFileName(path);
		comp.NextSaveTime = _timing.RealTime + TimeSpan.FromSeconds(_cfg.GetCVar(CCVars.AutosaveInterval));

        Log.Info($"Enabled autosaving for map (or grid) {path} ({ToPrettyString(uid)}). Next save in {ReadableTimeLeft(uid)} seconds.");
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
