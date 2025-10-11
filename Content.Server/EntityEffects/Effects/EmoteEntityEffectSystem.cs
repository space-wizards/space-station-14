using Content.Server.Chat.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects;

namespace Content.Server.EntityEffects.Effects;

/// <summary>
/// Makes this entity emote.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class EmoteEntityEffectSystem : EntityEffectSystem<MetaDataComponent, Emote>
{
    [Dependency] private readonly ChatSystem _chat = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<Emote> args)
    {
        if (args.Effect.ShowInChat)
            _chat.TryEmoteWithChat(entity, args.Effect.EmoteId, ChatTransmitRange.GhostRangeLimit, forceEmote: args.Effect.Force);
        else
            _chat.TryEmoteWithoutChat(entity, args.Effect.EmoteId);
    }
}
