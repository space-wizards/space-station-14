using Content.Shared.EntityEffects.Effects;
using Content.Shared.Popups;
using Robust.Shared.Random;

namespace Content.Shared.EntityEffects.NewEffects.Transform;

public sealed partial class PopupMessageEntityEffectSystem : EntityEffectSystem<TransformComponent, PopupMessage>
{
    // TODO: This will mispredict hard on client maybe use random workaround.
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<PopupMessage> args)
    {
        var msg = _random.Pick(args.Effect.Messages);

        // TODO: A way to pass arguments to this that aren't hardcoded into the effect.
        if (args.Effect.Type == PopupRecipients.Local)
            _popup.PopupEntity(Loc.GetString(msg, ("entity", entity)), entity, entity, args.Effect.VisualType);
        else if (args.Effect.Type == PopupRecipients.Pvs)
            _popup.PopupEntity(Loc.GetString(msg, ("entity", entity)), entity, args.Effect.VisualType);
    }
}

public sealed class PopupMessage : EntityEffectBase<PopupMessage>
{
    [DataField(required: true)]
    public string[] Messages = default!;

    [DataField]
    public PopupRecipients Type = PopupRecipients.Local;

    [DataField]
    public PopupType VisualType = PopupType.Small;
}
