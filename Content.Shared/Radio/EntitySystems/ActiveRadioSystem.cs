using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Popups;
using Content.Shared.Radio.Components;
using Robust.Shared.Configuration;

namespace Content.Shared.Radio.EntitySystems;

public sealed partial class ActiveRadioSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IConfigurationManager _configurationManager = default!;

    [SubscribeLocalEvent]
    private void OnRadioSendAttempt(Entity<ActiveRadioComponent> ent, ref RadioSendAttemptEvent args)
    {
        if (args.Channel.ID == SharedChatSystem.CommonChannel
            && !_configurationManager.GetCVar(CCVars.ChatCommonRadioChannelAllowHeadset)
            && !ent.Comp.CanSendInReceiveOnlyChannels)
        {
            _popup.PopupEntity(Loc.GetString("chat-manager-radio-channel-forbidden-for-headset"), ent, ent);
            args.Cancelled = true;
        }
    }
}
