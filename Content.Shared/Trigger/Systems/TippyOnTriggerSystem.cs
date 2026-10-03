using Content.Shared.Tips;
using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.Player;

namespace Content.Shared.Trigger.Systems;

public sealed partial class TippyOnTriggerSystem : XOnTriggerSystem<TippyOnTriggerComponent>
{
    [Dependency] private SharedTipsSystem _tips = default!;

    protected override void OnTrigger(Entity<TippyOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        var msg = ent.Comp.Message;
        var prototype = ent.Comp.Prototype;

        if (ent.Comp.LocMessage != null)
            msg = Loc.GetString(ent.Comp.LocMessage.Value);

        if (ent.Comp.UseOwnerPrototype)
            prototype = Prototype(ent)?.ID;

        var speakTime = ent.Comp.SpeakTime ?? _tips.GetSpeechTime(msg);

        if (ent.Comp.SendToAll)
        {
            _tips.SendTippy(msg, prototype, speakTime, ent.Comp.SlideTime, ent.Comp.WaddleInterval);
        }
        else
        {
            if (!TryComp<ActorComponent>(target, out var actor))
                return;

            _tips.SendTippy(actor.PlayerSession, msg, prototype, speakTime, ent.Comp.SlideTime, ent.Comp.WaddleInterval);
        }

        args.Handled = true;
    }
}
