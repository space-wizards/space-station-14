using Content.Shared.CharacterAppearance;
using Content.Shared.Markings;
using Content.Shared.Preferences;

namespace Content.Shared.Humanoid;

/// <summary>
///     HumanoidSystem. Primarily deals with the appearance and visual data
///     of a humanoid entity. HumanoidVisualizer is what deals with actually
///     organizing the sprites and setting up the sprite component's layers.
///
///     This is a shared system, because while it is server authoritative,
///     you still need a local copy so that players can set up their
///     characters.
/// </summary>
public abstract class SharedHumanoidSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;

    public void SetAppearance(EntityUid uid,
        string species,
        Dictionary<HumanoidVisualLayers, SharedHumanoidComponent.CustomBaseLayerInfo> customBaseLayer,
        Color skinColor,
        List<HumanoidVisualLayers> visLayers,
        List<Marking> markings)
    {
        var data = new HumanoidVisualizerData(species, customBaseLayer, skinColor, visLayers, markings);
        _appearance.SetData(uid, HumanoidVisualizerKey.Key, data);
    }
}
