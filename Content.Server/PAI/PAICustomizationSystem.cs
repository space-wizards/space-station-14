using Content.Shared.PAI;
using Robust.Server.GameObjects;

namespace Content.Server.PAI;

public sealed partial class PAICustomizationSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PAICustomizationComponent, ComponentInit>(Customization);
        SubscribeLocalEvent<PAIComponent, PAIEmotionMessage>(OnEmotionMessage);
    }

    private void Customization(Entity<PAICustomizationComponent> ent, ref ComponentInit args)
    {
        _appearance.SetData(ent.Owner, PAIEmotionVisuals.Emotion, ent.Comp.CurrentEmotion);
    }

    private void OnEmotionMessage(Entity<PAIComponent> ent, ref PAIEmotionMessage args)
    {
        if (!TryComp<PAICustomizationComponent>(ent.Owner, out var emotionComp))
            return;

        if (emotionComp.CurrentEmotion == args.Emotion)
            return;

        emotionComp.CurrentEmotion = args.Emotion;
        Dirty(ent.Owner, emotionComp);

        _appearance.SetData(ent.Owner, PAIEmotionVisuals.Emotion, args.Emotion);

        _ui.ServerSendUiMessage(ent.Owner, PAICustomizationUiKey.Key, new PAIEmotionStateMessage(args.Emotion));
    }
}
