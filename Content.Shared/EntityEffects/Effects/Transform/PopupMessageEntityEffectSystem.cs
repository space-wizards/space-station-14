using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Shared.EntityEffects.Effects.Transform;

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

        // TODO: A way to pass arguments to this that aren't hardcoded into the effect.
        if (args.Effect.Type == PopupRecipients.Local)
            _popup.PopupEntity(Loc.GetString(msg, ("entity", entity)), entity, entity, args.Effect.VisualType);
        else if (args.Effect.Type == PopupRecipients.Pvs)
            _popup.PopupEntity(Loc.GetString(msg, ("entity", entity)), entity, args.Effect.VisualType);
    }
}

public sealed partial class PopupMessage : EntityEffectBase<PopupMessage>
{
    [DataField(required: true)]
    public string[] Messages = default!;

    [DataField]
    public PopupRecipients Type = PopupRecipients.Local;

    [DataField]
    public PopupType VisualType = PopupType.Small;
}

public enum PopupRecipients
{
    Pvs,
    Local
}
