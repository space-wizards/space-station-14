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
                RemCompDeferred<AutoSaveComponent>(uid);
                continue;
            }

			if (!HasComp<MapComponent>(uid) && !HasComp<MapGridComponent>(uid))
			{
				Log.Warning($"Can't autosave entity {ToPrettyString(uid)}; it is not a map or grid. Removing component.");
				RemCompDeferred<AutoSaveComponent>(uid);
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

        Log.Info($"Enabled autosaving for map (or grid) {path} ({ToPrettyString(uid)}). Next save in {ReadableTimeLeft((uid, comp))} seconds.");
    }

    #endregion
}
