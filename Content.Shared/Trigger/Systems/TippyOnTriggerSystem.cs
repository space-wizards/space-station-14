using Content.Shared.Tips;
using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.Player;

namespace Content.Shared.Trigger.Systems;

public sealed class TippyOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly SharedTipsSystem _tips = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TippyOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(Entity<TippyOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

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
            var target = ent.Comp.TargetUser ? args.User : ent.Owner;
            if (!TryComp<ActorComponent>(target, out var actor))
                return;

            _tips.SendTippy(actor.PlayerSession, msg, prototype, speakTime, ent.Comp.SlideTime, ent.Comp.WaddleInterval);
        }

        args.Handled = true;
    }
}
