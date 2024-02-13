using Content.Shared.Chat.V2;
using Content.Shared.Chat.V2.Components;
using Robust.Shared.Player;

namespace Content.Server.Radio.EntitySystems;

public sealed class InternalRadioSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InternalRadioComponent, EntityRadioLocalEvent>(OnInternalRadioReceive);
    }

    private void OnInternalRadioReceive(EntityUid uid, InternalRadioComponent _, ref EntityRadioLocalEvent ev)
    {
        if (!TryComp<ActorComponent>(uid, out var actor))
            return;

        var translated = new EntityRadioedEvent(
            GetNetEntity(ev.Speaker),
            ev.AsName,
            ev.Message,
            ev.Channel,
            ev.Verb,
            ev.FontId,
            ev.FontSize,
            ev.IsBold,
            ev.IsAnnouncement,
            ev.MessageColorOverride
        );

        RaiseNetworkEvent(translated, actor.PlayerSession);
    }
}
