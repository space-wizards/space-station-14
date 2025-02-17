using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Content.Shared.Speech.Muting;
using Content.Shared.Traits.Assorted;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Chat.Systems;

public sealed class EmoteOnDamageSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmoteOnDamageComponent, DamageChangedEvent>(OnDamage);
    }

    private void OnDamage(EntityUid uid, EmoteOnDamageComponent emoteOnDamage, DamageChangedEvent args)
    {
        if (!args.DamageIncreased)
            return;

        if (emoteOnDamage.LastEmoteTime + emoteOnDamage.EmoteCooldown > _gameTiming.CurTime)
            return;

        // DS14-start
        if (HasComp<MutedComponent>(uid) || HasComp<PainNumbnessComponent>(uid))
            return;
        // DS14-end

        if (emoteOnDamage.Emotes.Count == 0)
            return;

        if (!_random.Prob(emoteOnDamage.EmoteChance))
            return;

        // DS14-start
        if (emoteOnDamage.ValidDamageGroups != null && args.DamageDelta != null)
        {
            foreach (var (group, _) in args.DamageDelta.GetDamagePerGroup(_prototype))
            {
                if (!emoteOnDamage.ValidDamageGroups.Contains(group))
                    return;
                if (args.DamageDelta.GetTotal() < 8)
                    return;
            }
        }
        // DS14-end

        var emote = _random.Pick(emoteOnDamage.Emotes);
        if (emoteOnDamage.WithChat)
        {
            _chatSystem.TryEmoteWithChat(uid, emote, emoteOnDamage.HiddenFromChatWindow ? ChatTransmitRange.HideChat : ChatTransmitRange.Normal);
        }
        else
        {
            _chatSystem.TryEmoteWithoutChat(uid, emote);
        }

        emoteOnDamage.LastEmoteTime = _gameTiming.CurTime;
    }

    /// <summary>
    /// Try to add an emote to the entity, which will be performed at an interval.
    /// </summary>
    public bool AddEmote(EntityUid uid, string emotePrototypeId, EmoteOnDamageComponent? emoteOnDamage = null)
    {
        if (!Resolve(uid, ref emoteOnDamage, logMissing: false))
            return false;

        DebugTools.Assert(emoteOnDamage.LifeStage <= ComponentLifeStage.Running);
        DebugTools.Assert(_prototypeManager.HasIndex<EmotePrototype>(emotePrototypeId), "Prototype not found. Did you make a typo?");

        return emoteOnDamage.Emotes.Add(emotePrototypeId);
    }

    /// <summary>
    /// Stop preforming an emote. Note that by default this will queue empty components for removal.
    /// </summary>
    public bool RemoveEmote(EntityUid uid, string emotePrototypeId, EmoteOnDamageComponent? emoteOnDamage = null, bool removeEmpty = true)
    {
        if (!Resolve(uid, ref emoteOnDamage, logMissing: false))
            return false;

        DebugTools.Assert(_prototypeManager.HasIndex<EmotePrototype>(emotePrototypeId), "Prototype not found. Did you make a typo?");

        if (!emoteOnDamage.Emotes.Remove(emotePrototypeId))
            return false;

        if (removeEmpty && emoteOnDamage.Emotes.Count == 0)
            RemCompDeferred(uid, emoteOnDamage);

        return true;
    }
}
