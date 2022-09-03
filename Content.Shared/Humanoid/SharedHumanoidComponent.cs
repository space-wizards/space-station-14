using System.Linq;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Humanoid;

public abstract class SharedHumanoidComponent : Component
{
    /// <summary>
    ///     Current species. Dictates things like base body sprites,
    ///     base humanoid to spawn, etc.
    /// </summary>
    [DataField("species")]
    public string Species { get; set; } = default!;

    /// <summary>
    ///     The initial profile and base layers to apply to this humanoid.
    /// </summary>
    [DataField("initial")]
    public string Initial { get; } = default!;

    /// <summary>
    ///     Skin color of this humanoid.
    /// </summary>
    [DataField("skinColor")]
    public Color SkinColor { get; set;  } = Color.FromHex("#C0967F");

    /// <summary>
    ///     Visual layers currently hidden. This will affect the base sprite
    ///     on this humanoid layer, and any markings that sit above it.
    /// </summary>
    [ViewVariables]
    public readonly HashSet<HumanoidVisualLayers> HiddenLayers = new();

    [DataField("sex")]
    public Sex Sex = Sex.Male;
}

[DataDefinition]
[Serializable, NetSerializable]
public sealed class CustomBaseLayerInfo
{
    public CustomBaseLayerInfo(string id, Color color)
    {
        ID = id;
        Color = color;
    }

    /// <summary>
    ///     ID of this custom base layer. Must be a <see cref="HumanoidSpeciesSpriteLayer"/>.
    /// </summary>
    [DataField("id")]
    public string ID { get; }

    /// <summary>
    ///     Color of this custom base layer.
    /// </summary>
    [DataField("color")]
    public Color Color { get; }
}
