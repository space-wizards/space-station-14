using Robust.Shared.Serialization;

namespace Content.Shared.Sprite;

/// <summary>
/// Sets the color of an entity to the parent's color when the entity is spawned via butchery.
/// Used to allow for color to persist between items. For example, a yellow animal butchered for
/// its pelt would remain yellow, even if its base sprite is white.
/// </summary>
[RegisterComponent]
public sealed partial class InheritColorOnSpawnComponent : Component
{
    [DataField]
    public string SourceVisualLayer = "enum.DamageStateVisualLayers.Base";

    [DataField]
    public List<string> DestinationVisualLayers = new() { "enum.DamageStateVisualLayers.Base" };
}

[Serializable, NetSerializable]
public enum InheritedColor
{
    Default,
}
