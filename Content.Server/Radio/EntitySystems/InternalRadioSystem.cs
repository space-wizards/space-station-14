using Content.Server.Chat.V2;
using Content.Shared.Chat.V2;
using Content.Shared.Chat.V2.Components;
using Robust.Shared.Player;

namespace Content.Server.Radio.EntitySystems;

public sealed class InternalRadioSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InternalRadioComponent, RadioSuccessEvent>(OnInternalRadioReceive);
    }

    private void OnInternalRadioReceive(EntityUid uid, InternalRadioComponent _, ref RadioSuccessEvent ev)
    {
        if (!TryComp<ActorComponent>(uid, out var actor))
            return;

        var translated = new RadioEvent(
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
