using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Robust.Shared.Prototypes;

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
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public const string DefaultSpecies = "Human";

    public void SetAppearance(EntityUid uid,
        string species,
        Dictionary<HumanoidVisualLayers, CustomBaseLayerInfo> customBaseLayer,
        Color skinColor,
        Sex sex,
        List<HumanoidVisualLayers> visLayers,
        List<Marking> markings)
    {
        var data = new HumanoidVisualizerData(species, customBaseLayer, skinColor, sex, visLayers, markings);

        // Locally raise an event for this, because there might be some systems interested
        // in this.
        RaiseLocalEvent(uid, new HumanoidAppearanceUpdateEvent(data), true);
        _appearance.SetData(uid, HumanoidVisualizerKey.Key, data);
    }
}

public sealed class HumanoidAppearanceUpdateEvent : EntityEventArgs
{
    public HumanoidVisualizerData Data { get; }

    public HumanoidAppearanceUpdateEvent(HumanoidVisualizerData data)
    {
        Data = data;
    }
}
