using Content.Server.Chat.Systems;
using Content.Server.Ghost.Components;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Ghost;

public sealed class SpookySpeakerSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpookySpeakerComponent, GhostBooEvent>(OnGhostBoo);
    }

    private void OnGhostBoo(Entity<SpookySpeakerComponent> entity, ref GhostBooEvent args)
    {
        // Only activate sometimes, so groups don't all trigger together
        if (!_random.Prob(entity.Comp.SpeakChance))
            return;

        var curTime = _timing.CurTime;
        // Enforce a delay between messages to prevent spam
        if (curTime < entity.Comp.NextSpeakTime)
            return;

        if (!_proto.TryIndex(entity.Comp.MessageSet, out var messages))
            return;

        // Grab a random localized message from the set
        var message = _random.Pick(messages);
        // Chatcode moment: messages starting with '.' are considered radio messages unless prefixed with '>'
        // So this is a stupid trick to make the "...Oooo"-style messages work.
        message = '>' + message;
        // Say the message
        _chat.TrySendInGameICMessage(entity, message, InGameICChatType.Speak, hideChat: true);

        // Set the delay for the next message
        entity.Comp.NextSpeakTime = curTime + entity.Comp.Cooldown;

        args.Handled = true;
    }
}
