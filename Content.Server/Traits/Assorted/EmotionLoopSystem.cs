using Robust.Shared.Random;
using Content.Server.Chat.Systems;

namespace Content.Server.Traits.Assorted;

/// <summary>
/// This system allows triggering any emotion at random intervals.
/// </summary>
public sealed class EmotionLoopSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<EmotionLoopComponent, ComponentStartup>(SetupTimer);
    }

    private void SetupTimer(EntityUid uid, EmotionLoopComponent component, ComponentStartup args)
    {
        component.NextIncidentTime = TimeSpan.FromSeconds(_random.NextFloat(component.MinTimeBetweenEmotions, component.MaxTimeBetweenEmotions));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<EmotionLoopComponent>();
        while (query.MoveNext(out var uid, out var emotionLoop))
        {
            emotionLoop.NextIncidentTime = TimeSpan.FromSeconds((float)emotionLoop.NextIncidentTime.TotalSeconds - frameTime);

            if (emotionLoop.NextIncidentTime >= TimeSpan.Zero)
                continue;

            // Set the updated time.
            emotionLoop.NextIncidentTime += TimeSpan.FromSeconds(_random.NextFloat(emotionLoop.MinTimeBetweenEmotions, emotionLoop.MaxTimeBetweenEmotions));

            // The next emotion to play is selected by randomly generating the index of an emotion.
            emotionLoop.NextEmotionIndex = _random.Next(0, emotionLoop.Emotions.Count);

            // Play the emotion by index "NextEmotionIndex".
            _chat.TryEmoteWithChat(uid, emotionLoop.Emotions[emotionLoop.NextEmotionIndex], ignoreActionBlocker: false);
        }
    }
}
