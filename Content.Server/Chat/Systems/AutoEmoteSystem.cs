namespace Content.Server.Chat.Systems;

using Content.Shared.Chat.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

public sealed class AutoEmoteSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AutoEmoteComponent, ComponentInit>(OnComponentInit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var autoEmote in EntityQuery<AutoEmoteComponent>())
        {
            var uid = autoEmote.Owner;

            if (autoEmote.NextEmoteTime > _gameTiming.CurTime)
                continue;

            foreach ((var key, var time) in autoEmote.EmoteTimers)
            {
                if (time > _gameTiming.CurTime)
                    continue;

                ResetTimer(uid, key, autoEmote);

                var autoEmotePrototype = _prototypeManager.Index<AutoEmotePrototype>(key);
                if (!_random.Prob(autoEmotePrototype.Chance))
                    continue;

                if (autoEmotePrototype.WithChat)
                {
                    _chatSystem.TryEmoteWithChat(uid, autoEmotePrototype.EmoteId, autoEmotePrototype.HiddenFromChatWindow);
                }
                else
                {
                    _chatSystem.TryEmoteWithoutChat(uid, autoEmotePrototype.EmoteId);
                }
            }
        }
    }

    private void OnComponentInit(EntityUid uid, AutoEmoteComponent autoEmote, ComponentInit args)
    {
        // Start timers
        foreach (var autoEmotePrototypeId in autoEmote.Emotes)
        {
            ResetTimer(uid, autoEmotePrototypeId, autoEmote);
        }
    }

    /// <summary>
    /// Try to add an emote to the entity, which will be preformed at an interval.
    /// </summary>
    public bool AddEmote(EntityUid uid, string autoEmotePrototypeId, AutoEmoteComponent? autoEmote = null)
    {
        if (!Resolve(uid, ref autoEmote))
            return false;

        if (autoEmote.Emotes.Contains(autoEmotePrototypeId))
            return false;


        autoEmote.Emotes.Add(autoEmotePrototypeId);
        ResetTimer(uid, autoEmotePrototypeId, autoEmote);

        return true;
    }

    /// <summary>
    /// Stop performing an emote.
    /// </summary>
    public bool RemoveEmote(EntityUid uid, string autoEmotePrototypeId, AutoEmoteComponent? autoEmote = null)
    {
        if (!Resolve(uid, ref autoEmote))
            return false;

        autoEmote.EmoteTimers.Remove(autoEmotePrototypeId);
        return autoEmote.Emotes.Remove(autoEmotePrototypeId);
    }

    /// <summary>
    /// Reset the timer for a specific emote, or return false if it doesn't exist.
    /// </summary>
    public bool ResetTimer(EntityUid uid, string autoEmotePrototypeId, AutoEmoteComponent? autoEmote = null)
    {
        if (!Resolve(uid, ref autoEmote))
            return false;

        if (!autoEmote.Emotes.Contains(autoEmotePrototypeId))
            return false;

        var autoEmotePrototype = _prototypeManager.Index<AutoEmotePrototype>(autoEmotePrototypeId);
        var time = _gameTiming.CurTime + autoEmotePrototype.Interval;
        autoEmote.EmoteTimers[autoEmotePrototypeId] = time;

        if (autoEmote.NextEmoteTime > time || autoEmote.NextEmoteTime <= _gameTiming.CurTime)
            autoEmote.NextEmoteTime = time;

        return true;
    }
}
