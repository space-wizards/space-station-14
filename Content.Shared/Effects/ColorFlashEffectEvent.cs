using Robust.Shared.Serialization;

namespace Content.Shared.Effects;

/// <summary>
/// Raised on the server and sent to a client to play the color flash animation.
/// </summary>
[Serializable, NetSerializable]
public sealed class ColorFlashEffectEvent : EntityEventArgs
{
    /// <summary>
    /// Color to play for the flash.
    /// </summary>
    public Color Color;

    public List<NetEntity> Entities;

    /// <summary>
    /// A string representing where the event came from.
    /// The color can be overridden per EffectSource on an entity using the ColorFlashEffectOverrideComponent
    /// </summary>
    public string EffectSource;

    public ColorFlashEffectEvent(string source, Color color, List<NetEntity> entities)
    {
        Color = color;
        Entities = entities;
        EffectSource = source;
    }
}
