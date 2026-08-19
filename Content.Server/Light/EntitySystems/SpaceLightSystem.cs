using Content.Server.GameTicking;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.Light.EntitySystems;

/// <summary>
/// Applies `starlight` to space maps while preserving explicit per-map light setups.
/// </summary>
public sealed partial class SpaceLightSystem : EntitySystem
{
    // This really just exists to avoid doing mapping changes for all of them.

    private Color _spaceLightColor;
    private readonly HashSet<MapId> _spaceLightMaps = new();

    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private SharedMapSystem _map = default!;

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_cfg, CCVars.SpaceLightColor, OnSpaceLightColorChanged, true);

        SubscribeLocalEvent<PostGameMapLoad>(OnPostGameMapLoad);
        SubscribeLocalEvent<MapRemovedEvent>(OnMapRemoved);
    }

    private void OnPostGameMapLoad(PostGameMapLoad ev)
    {
        if (!_map.TryGetMap(ev.Map, out var mapUid))
            return;

        // MapLight is often intentionally set for planet/salvage/arena maps, so don't overwrite it.
        if (HasComp<MapLightComponent>(mapUid.Value))
            return;

        _spaceLightMaps.Add(ev.Map);
        _map.SetAmbientLight(ev.Map, _spaceLightColor);
    }

    private void OnMapRemoved(MapRemovedEvent ev)
    {
        _spaceLightMaps.Remove(ev.MapId);
    }

    private void OnSpaceLightColorChanged(string value)
    {
        _spaceLightColor = ParseSpaceLightColor(value);

        foreach (var mapId in _spaceLightMaps)
        {
            if (!_map.MapExists(mapId))
                continue;

            _map.SetAmbientLight(mapId, _spaceLightColor);
        }
    }

    private static Color ParseSpaceLightColor(string value)
    {
        if (!Color.TryFromHex(value, out var color))
            color = Color.FromHex(CCVars.DefaultSpaceLightColor);

        return Color.FromSrgb(color);
    }
}
