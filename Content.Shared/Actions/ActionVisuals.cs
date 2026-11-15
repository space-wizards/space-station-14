using Robust.Shared.Serialization;

namespace Content.Shared.Actions;

/// <summary>
///     Sprite layer keys for action entities.
/// </summary>
[Serializable, NetSerializable]
public enum ActionVisuals : byte
{
    /// <summary>Layer key. The action's base icon.</summary>
    Icon,

    /// <summary>Layer key. Optional, shown only while toggled on.</summary>
    IconToggled,
}

/// <summary>
///     Appearance data keys for action entities.
/// </summary>
[Serializable, NetSerializable]
public enum ActionState : byte
{
    /// <summary>Appearance key. Mirrors ActionComponent.Toggled.</summary>
    Toggled,

    /// <summary>Appearance key. Runtime-assigned SpriteSpecifier, for actions with a dynamic icon.</summary>
    DynamicIcon,

    /// <summary>Appearance key. Runtime-assigned tint override, e.g. decal-placement actions.</summary>
    Color,
}
