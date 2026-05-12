using Content.Server.Chat.Systems;
using Content.Shared.Chat;
using Content.Shared.Trigger;
using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Trigger.Systems;

public sealed class SpeakOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpeakOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(Entity<SpeakOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        var target = ent.Comp.TargetUser ? args.User : ent.Owner;

        if (target == null)
            return;

        string message;
        if (ent.Comp.Text != null)
            message = Loc.GetString(ent.Comp.Text);
        else
        {
            if (!_prototypeManager.Resolve(ent.Comp.Pack, out var messagePack))
                return;
            message = Loc.GetString(_random.Pick(messagePack.Values));
        }
        // Chatcode moment: messages starting with "." are considered radio messages.
        // Prepending ">" forces the message to be spoken instead.
        // TODO chat refactor: remove this
        message = '>' + message;
        _chat.TrySendInGameICMessage(target.Value, message, InGameICChatType.Speak, true);
        args.Handled = true;
    }
}
