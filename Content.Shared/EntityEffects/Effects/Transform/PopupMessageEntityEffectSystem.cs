using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Shared.EntityEffects.Effects.Transform;

/// <summary>
/// Creates a text popup to appear at this entity's coordinates.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PopupMessageEntityEffectSystem : EntityEffectSystem<TransformComponent, PopupMessage>
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    protected override void Effect(Entity<TransformComponent> entity, PopupMessage effect, EntityEffectData data)
    {
        // TODO: When we get proper random prediction remove this check.
        if (_net.IsClient)
            return;

        var msg = Loc.GetString(_random.Pick(effect.Messages), ("entity", entity));

        switch ((effect.Method, effect.Type))
        {
            case (PopupMethod.PopupEntity, PopupRecipients.Local):
                _popup.PopupEntity(msg, entity, entity, effect.VisualType);
                break;
            case (PopupMethod.PopupEntity, PopupRecipients.Pvs):
                _popup.PopupEntity(msg, entity, effect.VisualType);
                break;
            case (PopupMethod.PopupCoordinates, PopupRecipients.Local):
                _popup.PopupCoordinates(msg, Transform(entity).Coordinates, entity, effect.VisualType);
                break;
            case (PopupMethod.PopupCoordinates, PopupRecipients.Pvs):
                _popup.PopupCoordinates(msg, Transform(entity).Coordinates, effect.VisualType);
                break;
        }
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class PopupMessage : EntityEffect
{
    /// <summary>
    /// Array of messages that can popup.
    /// Only one is chosen when the effect is applied.
    /// </summary>
    [DataField(required: true)]
    public string[] Messages = default!;

    /// <summary>
    /// Whether to just the entity we're affecting, or everyone around them.
    /// </summary>
    [DataField]
    public PopupRecipients Type = PopupRecipients.Local;

    /// <summary>
    /// Which popup API method to use.
    /// Use PopupCoordinates in case the entity will be deleted while the popup is shown.
    /// </summary>
    [DataField]
    public PopupMethod Method = PopupMethod.PopupEntity;

    /// <summary>
    /// Size of the popup.
    /// </summary>
    [DataField]
    public PopupType VisualType = PopupType.Small;
}

[Serializable, NetSerializable]
public enum PopupRecipients : byte
{
    Pvs,
    Local,
}

[Serializable, NetSerializable]
public enum PopupMethod : byte
{
    PopupEntity,
    PopupCoordinates,
}
