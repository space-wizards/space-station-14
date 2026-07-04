using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Nutrition.Components;

/// <summary>
/// Allows this entity to be hit by banana cream pies.
/// See <see cref="CreamPieComponent"/>.
/// </summary>
[Access(typeof(SharedCreamPieSystem))]
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class CreamPiedComponent : Component
{
    /// <summary>
    /// Was this entity hit by a banana cream pie?
    /// This is reset if they get splashed with water.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CreamPied;

    /// <summary>
    /// The sprite to draw on someone's face if they were hit by a pie.
    /// The layer will be dynamically added with the component.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SpriteSpecifier? Sprite;
}

/// <summary>
/// Key to be used with appearance data, indicating if the entity has a banana cream pie in their face.
/// </summary>
[Serializable, NetSerializable]
public enum CreamPiedVisuals
{
    Creamed,
}

/// <summary>
/// The visual layer for the creampied face.
/// Will be dynamically added and removed with the component.
/// </summary>
[Serializable, NetSerializable]
public enum CreamPiedVisualLayer
{
    Key,
}
