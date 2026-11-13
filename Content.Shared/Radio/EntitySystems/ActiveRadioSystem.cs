using Content.Shared.Popups;
using Content.Shared.Radio.Components;

namespace Content.Shared.Radio.EntitySystems;

public sealed partial class ActiveRadioSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;

    [SubscribeLocalEvent]
    private void OnRadioSendAttempt(Entity<ActiveRadioComponent> ent, ref RadioSendAttemptEvent args)
    {
        if (!args.Channel.AllowHeadsetSend && !ent.Comp.CanSendInReceiveOnlyChannels)
        {
            _popup.PopupEntity(Loc.GetString("chat-manager-radio-channel-forbidden-for-headset"), ent, ent);
            args.Cancelled = true;
        }
    }
}
