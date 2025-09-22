using Content.Shared.Chat.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

public abstract partial class SharedEmoteEntityEffectSystem : EntityEffectSystem<MetaDataComponent, Emote>
{
    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<Emote> args)
    {
        // Server side system
    }
}

[DataDefinition]
public sealed partial class Emote : EntityEffectBase<Emote>
{
    /// <summary>
    ///     The emote the entity will preform.
    /// </summary>
    [DataField("emote", required: true)]
    public ProtoId<EmotePrototype> EmoteId;

    /// <summary>
    ///     If the emote should be recorded in chat.
    /// </summary>
    [DataField]
    public bool ShowInChat;

    /// <summary>
    ///     If the forced emote will be listed in the guidebook.
    /// </summary>
    [DataField]
    public bool ShowInGuidebook;

    /// <summary>
    ///     If true, the entity will preform the emote even if they normally can't.
    /// </summary>
    [DataField]
    public bool Force;
}
