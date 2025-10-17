using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Shared.EntityEffects.Effects.Transform;

/// <summary>
/// Creates a text popup to appear at this entity's coordinates.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PopupMessageEntityEffectSystem : EntityEffectSystem<TransformComponent, PopupMessage>
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<PopupMessage> args)
    {
        // TODO: When we get proper random prediction remove this check.
        if (_net.IsClient)
            return;

        var msg = _random.Pick(args.Effect.Messages);

        switch (args.Effect.Type)
        {
            case PopupRecipients.Local:
                _popup.PopupEntity(Loc.GetString(msg, ("entity", entity)), entity, entity, args.Effect.VisualType);
                break;
            case PopupRecipients.Pvs:
                _popup.PopupEntity(Loc.GetString(msg, ("entity", entity)), entity, args.Effect.VisualType);
                break;
        }
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class PopupMessage : EntityEffectBase<PopupMessage>
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
    /// Size of the popup.
    /// </summary>
    [DataField]
    public PopupType VisualType = PopupType.Small;
}

public enum PopupRecipients
{
    Pvs,
    Local
}
