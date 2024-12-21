namespace Content.Server.Traits.Assorted;

using Robust.Shared.Random;
using Content.Server.Chat.Systems;

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
            // If the HashSet "Emotes" is empty, exit this system.
            if (emotionLoop.Emotes.Count == 0)
                return;

            emotionLoop.NextIncidentTime = TimeSpan.FromSeconds((float)emotionLoop.NextIncidentTime.TotalSeconds - frameTime);

            if (emotionLoop.NextIncidentTime >= TimeSpan.Zero)
                continue;

            // Set the updated time.
            emotionLoop.NextIncidentTime += _random.Next(emotionLoop.MinTimeBetweenEmotions, emotionLoop.MaxTimeBetweenEmotions);

            // Select a random emotion from the HashSet "Emotes".
            var emote = _random.Pick(emotionLoop.Emotes);

            // Play the emotion recorded in "emote".
            _chat.TryEmoteWithChat(uid, emote, ignoreActionBlocker: false);
        }
    }
}
