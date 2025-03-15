using Content.Server.Chat.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Traits.Assorted;

/// <summary>
/// This system allows triggering any emotion at random intervals.
/// </summary>
public sealed class EmotionLoopSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<EmotionLoopComponent, ComponentStartup>(SetupTimer);
    }

    private void SetupTimer(Entity<EmotionLoopComponent> entity, ref ComponentStartup args)
    {
        entity.Comp.NextIncidentTime = _random.Next(entity.Comp.MinTimeBetweenEmotions, entity.Comp.MaxTimeBetweenEmotions);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<EmotionLoopComponent>();
        while (query.MoveNext(out var uid, out var emotionLoop))
        {
            if (emotionLoop.Emotes.Count == 0)
                return;

            if (_timing.CurTime < emotionLoop.NextIncidentTime)
                continue;

            emotionLoop.NextIncidentTime += _random.Next(emotionLoop.MinTimeBetweenEmotions, emotionLoop.MaxTimeBetweenEmotions);

            // Play the emotion by random index.
            _chat.TryEmoteWithChat(uid, emotionLoop.Emotes[_random.Next(0, emotionLoop.Emotes.Count)], ignoreActionBlocker: false);
        }
    }
}
