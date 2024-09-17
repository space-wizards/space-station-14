using Content.Shared.Speech;
using Content.Shared.Tag;
using Robust.Shared.Audio;

namespace Content.Server.Radio.EntitySystems;

public sealed partial class RadioMessageModifierSystem : EntitySystem
{
    [Dependency] private readonly TagSystem _tag = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RadioModifyMessageEvent>(OnRadioModifiyMessage);
    }

    public const string StationAiTag = "StationAi";
    public readonly SoundSpecifier RadioAiSound = new SoundPathSpecifier("/Audio/Voice/Talk/radio_ai.ogg", AudioParams.Default.WithVariation(0.125f));

    public void OnRadioModifiyMessage(ref RadioModifyMessageEvent args)
    {
        // Not sure how to check if the sender is the station AI, so instead i'm just gonna check for the tag...
        // Please replace this if you have a better way of knowing.
        if (_tag.HasTag(args.RadioSource, StationAiTag))
        {
            args.Message.Sound = RadioAiSound;
        }
    }
}
