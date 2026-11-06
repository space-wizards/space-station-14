using Content.Shared.Damage.Systems;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Chat.Systems;

/// <summary>
/// Handles entities emoting when taking damage.
/// </summary>
/// <seealso cref="EmoteOnDamageComponent"/>
public sealed partial class EmoteOnDamageSystem : EntitySystem
{
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ChatSystem _chatSystem = default!;

    [SubscribeLocalEvent]
    private void OnDamage(Entity<EmoteOnDamageComponent> ent, ref DamageDealtEvent args)
    {
        if (!args.AnyPositive)
            return;

        if (ent.Comp.LastEmoteTime + ent.Comp.EmoteCooldown > _gameTiming.CurTime)
            return;

        if (ent.Comp.Emotes.Count == 0)
            return;

        if (!_random.Prob(ent.Comp.EmoteChance))
            return;

        var emote = _random.Pick(ent.Comp.Emotes);
        if (ent.Comp.WithChat)
        {
            _chatSystem.TryEmoteWithChat(ent, emote, ent.Comp.HiddenFromChatWindow ? ChatTransmitRange.HideChat : ChatTransmitRange.Normal);
        }
        else
        {
            _chatSystem.TryEmoteWithoutChat(ent, emote);
        }

        ent.Comp.LastEmoteTime = _gameTiming.CurTime;
    }

    /// <summary>
    /// Try to add an emote to the entity, which will be performed at an interval.
    /// </summary>
    public bool AddEmote(EntityUid uid, string emotePrototypeId, EmoteOnDamageComponent? emoteOnDamage = null)
    {
        if (!Resolve(uid, ref emoteOnDamage, logMissing: false))
            return false;

        DebugTools.Assert(emoteOnDamage.LifeStage <= ComponentLifeStage.Running);
        DebugTools.Assert(ProtoMan.HasIndex<EmotePrototype>(emotePrototypeId), "Prototype not found. Did you make a typo?");

        return emoteOnDamage.Emotes.Add(emotePrototypeId);
    }

    /// <summary>
    /// Stop preforming an emote. Note that by default this will queue empty components for removal.
    /// </summary>
    public bool RemoveEmote(EntityUid uid, string emotePrototypeId, EmoteOnDamageComponent? emoteOnDamage = null, bool removeEmpty = true)
    {
        if (!Resolve(uid, ref emoteOnDamage, logMissing: false))
            return false;

        DebugTools.Assert(ProtoMan.HasIndex<EmotePrototype>(emotePrototypeId), "Prototype not found. Did you make a typo?");

        if (!emoteOnDamage.Emotes.Remove(emotePrototypeId))
            return false;

        if (removeEmpty && emoteOnDamage.Emotes.Count == 0)
            RemCompDeferred(uid, emoteOnDamage);

        return true;
    }
}
