namespace Content.Server.Chat.Systems;

using System.Linq;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

public sealed class AutoEmoteSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AutoEmoteComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<AutoEmoteComponent, EntityUnpausedEvent>(OnUnpaused);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _gameTiming.CurTime;
        foreach (var autoEmote in EntityQuery<AutoEmoteComponent>())
        {
            var uid = autoEmote.Owner;

            if (autoEmote.NextEmoteTime > curTime)
                continue;

            foreach ((var key, var time) in autoEmote.EmoteTimers)
            {
                if (time > curTime)
                    continue;

                var autoEmotePrototype = _prototypeManager.Index<AutoEmotePrototype>(key);
                ResetTimer(uid, key, autoEmote, autoEmotePrototype);

                if (!_random.Prob(autoEmotePrototype.Chance))
                    continue;

                if (autoEmotePrototype.WithChat)
                {
                    _chatSystem.TryEmoteWithChat(uid, autoEmotePrototype.EmoteId, autoEmotePrototype.HiddenFromChatWindow ? ChatTransmitRange.HideChat : ChatTransmitRange.Normal);
                }
                else
                {
                    _chatSystem.TryEmoteWithoutChat(uid, autoEmotePrototype.EmoteId);
                }
            }
        }
    }

    private void OnMapInit(EntityUid uid, AutoEmoteComponent autoEmote, MapInitEvent args)
    {
        // Start timers
        foreach (var autoEmotePrototypeId in autoEmote.Emotes)
        {
            ResetTimer(uid, autoEmotePrototypeId, autoEmote);
        }
    }

    private void OnUnpaused(EntityUid uid, AutoEmoteComponent autoEmote, ref EntityUnpausedEvent args)
    {
        foreach (var key in autoEmote.EmoteTimers.Keys)
        {
            autoEmote.EmoteTimers[key] += args.PausedTime;
        }
        autoEmote.NextEmoteTime += args.PausedTime;
    }

    /// <summary>
    /// Try to add an emote to the entity, which will be performed at an interval.
    /// </summary>
    public bool AddEmote(EntityUid uid, string autoEmotePrototypeId, AutoEmoteComponent? autoEmote = null)
    {
        if (!Resolve(uid, ref autoEmote, logMissing: false))
            return false;

        DebugTools.Assert(autoEmote.LifeStage <= ComponentLifeStage.Running);

        if (autoEmote.Emotes.Contains(autoEmotePrototypeId))
            return false;

        autoEmote.Emotes.Add(autoEmotePrototypeId);
        ResetTimer(uid, autoEmotePrototypeId, autoEmote);

        return true;
    }

    /// <summary>
    /// Stop preforming an emote. Note that by default this will queue empty components for removal.
    /// </summary>
    public bool RemoveEmote(EntityUid uid, string autoEmotePrototypeId, AutoEmoteComponent? autoEmote = null, bool removeEmpty = true)
    {
        if (!Resolve(uid, ref autoEmote, logMissing: false))
            return false;

        DebugTools.Assert(_prototypeManager.HasIndex<AutoEmotePrototype>(autoEmotePrototypeId), "Prototype not found. Did you make a typo?");

        if (!autoEmote.EmoteTimers.Remove(autoEmotePrototypeId))
            return false;

        if (autoEmote.EmoteTimers.Count > 0)
            autoEmote.NextEmoteTime = autoEmote.EmoteTimers.Values.Min();
        else if (removeEmpty)
            RemCompDeferred(uid, autoEmote);
        else
            autoEmote.NextEmoteTime = TimeSpan.MaxValue;

        return true;
    }

    /// <summary>
    /// Reset the timer for a specific emote, or return false if it doesn't exist.
    /// </summary>
    public bool ResetTimer(EntityUid uid, string autoEmotePrototypeId, AutoEmoteComponent? autoEmote = null, AutoEmotePrototype? autoEmotePrototype = null)
    {
        if (!Resolve(uid, ref autoEmote))
            return false;

        if (!autoEmote.Emotes.Contains(autoEmotePrototypeId))
            return false;

        autoEmotePrototype ??= _prototypeManager.Index<AutoEmotePrototype>(autoEmotePrototypeId);

        var curTime = _gameTiming.CurTime;
        var time = curTime + autoEmotePrototype.Interval;
        autoEmote.EmoteTimers[autoEmotePrototypeId] = time;

        if (autoEmote.NextEmoteTime > time || autoEmote.NextEmoteTime <= curTime)
            autoEmote.NextEmoteTime = time;

        return true;
    }
}
