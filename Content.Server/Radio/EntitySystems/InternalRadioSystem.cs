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

        SubscribeLocalEvent<InternalRadioComponent, RadioEmittedEvent>(OnInternalRadioReceive);
    }

    private void OnInternalRadioReceive(EntityUid uid, InternalRadioComponent _, ref RadioEmittedEvent ev)
    {
        if (!TryComp<ActorComponent>(uid, out var actor))
            return;

        RaiseNetworkEvent(new RadioEvent(GetNetEntity(ev.Speaker),ev.AsName,ev.Message,ev.Channel), actor.PlayerSession);
    }
}
