/// by ModerN, mailto:modern-nm@yandex.by or https://github.com/modern-nm. Discord: modern.df

using Content.Server.Chat.Systems;
using Content.Shared.ADT;
using Content.Shared.Interaction.Events;

namespace Content.Server.ADT;

/// <summary>
/// This system can be used for send emote-message to clients when some custom item was used.
/// </summary>
public sealed class EmitEmoteSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EmitEmoteOnUseComponent, UseInHandEvent>(handler: OnEmitEmoteOnUseInHand);
    }

    private void OnEmitEmoteOnUseInHand(EntityUid uid, EmitEmoteOnUseComponent component, UseInHandEvent args)
    {
        // Intentionally not checking whether the interaction has already been handled.
        TryEmitEmote(uid, component, args.User);

        if (component.Handle)
            args.Handled = true;
    }

    /// <summary>
    /// This method makes user of entity (if it has EmitEmoteOnUseComponent) to call emote.
    /// </summary>
    private void TryEmitEmote(EntityUid uid, EmitEmoteOnUseComponent component, EntityUid? user = null, bool predict = true)
    {
        if (user == null)
            return;
        if (component.EmoteType == null)
            return;

        if(EntityManager.TrySystem<ChatSystem>(out var chatSystem)) 
        {
            chatSystem.TryEmoteWithChat((EntityUid) user, component.EmoteType);
        }
    }
}
