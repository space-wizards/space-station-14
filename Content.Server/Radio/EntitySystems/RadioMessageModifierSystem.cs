using Content.Server.Radio.Components;
using Content.Shared.Speech;
using Content.Shared.Tag;
using Robust.Shared.Audio;

namespace Content.Server.Radio.EntitySystems;

public sealed partial class RadioMessageModifierSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RadioMessageSoundComponent, RadioModifyMessageEvent>(OnRadioModifiyMessage);
    }

    /// <summary>
    /// Changes a radio message sound based on the RadioMessageSoundComponent attached to the entity.
    /// </summary>
    public void OnRadioModifiyMessage(Entity<RadioMessageSoundComponent> entity, ref RadioModifyMessageEvent args)
    {
        args.Message.Sound = entity.Comp.Sound;
    }
}
