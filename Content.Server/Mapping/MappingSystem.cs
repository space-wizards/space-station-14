using System.IO;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
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
public sealed partial class MappingSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private IResourceManager _resMan = default!;
    [Dependency] private MapLoaderSystem _loader = default!;

    private bool _autosaveEnabled;

    public override void Initialize()
    {
        base.Initialize();
        Subs.CVar(_cfg, CCVars.AutosaveEnabled, b => _autosaveEnabled = b, true);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_autosaveEnabled)
            return;

        // Maps are paused while in mapping, so we have to use AllEntityQuery to get them.
		var query = AllEntityQuery<AutoSaveComponent>();
		while (query.MoveNext(out var uid, out var autoSave))
		{
			if (_timing.RealTime <= autoSave.NextSaveTime)
				continue;

            if (LifeStage(uid) >= EntityLifeStage.MapInitialized) // Saving post-init maps or grids has a high chance of throwing errors.
            {
                Log.Warning($"Can't autosave entity {ToPrettyString(uid)}; it is not paused. Removing component.");
                RemCompDeferred(uid, autoSave);
                continue;
            }

			if (!HasComp<MapComponent>(uid) && !HasComp<MapGridComponent>(uid))
			{
				Log.Warning($"Can't autosave entity {ToPrettyString(uid)}; it is not a map or grid. Removing component.");
				RemCompDeferred(uid, autoSave);
				continue;
			}

			autoSave.NextSaveTime = _timing.RealTime + TimeSpan.FromSeconds(_cfg.GetCVar(CCVars.AutosaveInterval));

			var saveDir = new ResPath(Path.Combine(_cfg.GetCVar(CCVars.AutosaveDirectory), autoSave.FileName).Replace(Path.DirectorySeparatorChar, '/'));
            _resMan.UserData.CreateDir(saveDir.ToRootedPath());

            var path = saveDir / new ResPath($"{DateTime.Now:yyyy-M-dd_HH.mm.ss}-AUTO.yml");
            Log.Info($"Autosaving map {autoSave.FileName} ({uid}) to {path}. Next save in {ReadableTimeLeft((uid, autoSave))} seconds.");

            if (HasComp<MapComponent>(uid))
                _loader.TrySaveMap(uid, path);
            else
                _loader.TrySaveGrid(uid, path);
		}
	}

    private double ReadableTimeLeft(Entity<AutoSaveComponent> ent)
    {
		return Math.Round(ent.Comp.NextSaveTime.TotalSeconds - _timing.RealTime.TotalSeconds);
	}

    /// <summary>
    /// Toggles autosaving of a map.
    /// </summary>
    /// <param name="map">Map ID of the map to autosave.</param>
    /// <param name="path">Relative path inside the user data folder to save into.</param>
    public bool ToggleAutosave(MapId map, string? path = null)
    {
        if (_map.TryGetMap(map, out var uid))
            return ToggleAutosave(uid.Value, path);

        Log.Error($"Tried to toggle autosave for an invalid MapID {map}!");
        return false;
    }

    /// <summary>
    /// Toggles autosaving of a map or a grid.
    /// </summary>
    /// <param name="uid">UID of the map or the grid to autosave.</param>
    /// <param name="path">Relative path inside the user data folder to save into.</param>
    public bool ToggleAutosave(EntityUid uid, string? path = null)
    {
        if (!_autosaveEnabled)
            return false;

		if (HasComp<AutoSaveComponent>(uid))
		{
            Log.Info($"Disabled autosaving for map (or grid) ({ToPrettyString(uid)}).");
			RemComp<AutoSaveComponent>(uid);
			return false;
		}

		if (!HasComp<MapComponent>(uid) && !HasComp<MapGridComponent>(uid))
		{
			Log.Error($"Tried to toggle autosave for {ToPrettyString(uid)}, but it is neither a grid or map!");
			return false;
		}

		var comp = EnsureComp<AutoSaveComponent>(uid);
		comp.FileName = Path.GetFileName(path ?? string.Empty);
		comp.NextSaveTime = _timing.RealTime + TimeSpan.FromSeconds(_cfg.GetCVar(CCVars.AutosaveInterval));

        Log.Info($"Enabled autosaving for map (or grid) {ToPrettyString(uid)} into path {path}. Next save in {ReadableTimeLeft((uid, comp))} seconds.");
        return true;
    }
}
