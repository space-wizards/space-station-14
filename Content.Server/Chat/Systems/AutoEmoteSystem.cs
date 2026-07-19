using System.Linq;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Chat.Systems;

public sealed partial class AutoEmoteSystem : EntitySystem
{
    private const string TimerPrefix = "emote:";

    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ChatSystem _chatSystem = default!;
    [Dependency] private EntityTimerSystem _timers = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AutoEmoteComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<AutoEmoteComponent, EntityUnpausedEvent>(OnUnpaused);
        SubscribeLocalEvent<AutoEmoteComponent, EntityTimerEvent>(OnTimer);
    }

    private void OnTimer(Entity<AutoEmoteComponent> ent, ref EntityTimerEvent args)
    {
        if (!args.Id.Value.StartsWith(TimerPrefix, StringComparison.Ordinal))
            return;

        var key = args.Id.Value[TimerPrefix.Length..];
        if (!ent.Comp.EmoteTimers.ContainsKey(key))
            return;

        var prototype = ProtoMan.Index<AutoEmotePrototype>(key);
        ResetTimer(ent, key, ent.Comp, prototype);
        if (!_random.Prob(prototype.Chance))
            return;

        if (prototype.WithChat)
            _chatSystem.TryEmoteWithChat(ent, prototype.EmoteId,
                prototype.HiddenFromChatWindow ? ChatTransmitRange.HideChat : ChatTransmitRange.Normal,
                ignoreActionBlocker: prototype.IgnoreActionBlocker, forceEmote: prototype.Force);
        else
            _chatSystem.TryEmoteWithoutChat(ent, prototype.EmoteId);
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

        DebugTools.Assert(ProtoMan.HasIndex<AutoEmotePrototype>(autoEmotePrototypeId), "Prototype not found. Did you make a typo?");

        if (!autoEmote.EmoteTimers.Remove(autoEmotePrototypeId))
            return false;

        _timers.CancelTimer<AutoEmoteComponent>(uid, TimerId(autoEmotePrototypeId));

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

        autoEmotePrototype ??= ProtoMan.Index<AutoEmotePrototype>(autoEmotePrototypeId);

        var curTime = _gameTiming.CurTime;
        var time = curTime + autoEmotePrototype.Interval;
        autoEmote.EmoteTimers[autoEmotePrototypeId] = time;
        _timers.SetTimerAt<AutoEmoteComponent>((uid, autoEmote), TimerId(autoEmotePrototypeId), time);

        if (autoEmote.NextEmoteTime > time || autoEmote.NextEmoteTime <= curTime)
            autoEmote.NextEmoteTime = time;

        return true;
    }

    private static EntityTimerId TimerId(string prototypeId) => new($"{TimerPrefix}{prototypeId}");
}
