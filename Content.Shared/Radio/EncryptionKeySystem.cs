using Content.Server.Radio.Components;
using Content.Shared.Examine;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;

namespace Content.Server.Radio.EntitySystems;

public sealed class EncryptionKeySystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EncryptionKeyComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(EntityUid uid, EncryptionKeyComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;
        if(component.Channels.Count > 0)
        {
            args.PushMarkup(Loc.GetString("examine-encryption-key-channels-prefix"));
            EncryptionKeySystem.GetChannelsExamine(component.Channels, args, _protoManager, "examine-headset-channel");
            if (component.DefaultChannel != null)
            {
                var proto = _protoManager.Index<RadioChannelPrototype>(component.DefaultChannel);
                args.PushMarkup(Loc.GetString("examine-encryption-key-default-channel", ("channel", component.DefaultChannel), ("color", proto.Color)));
            }
        }
    }

    /// <summary>
    ///     A static method for formating list of radio channels for examine events.
    /// </summary>
    /// <param name="channels">HashSet of channels in headset, encryptionkey or etc.</param>
    /// <param name="protoManager">IPrototypeManager for getting prototypes of channels with their variables.</param>
    /// <param name="channelFTLPattern">String that provide id of pattern in .ftl files to format channel with variables of it.</param>
    public static void GetChannelsExamine(HashSet<string> channels, ExaminedEvent examineEvent, IPrototypeManager protoManager, string channelFTLPattern)
    {
        foreach (var id in channels)
        {
            var proto = protoManager.Index<RadioChannelPrototype>(id);
            string keyCode = "" + proto.KeyCode;
            if (id != "Common")
                keyCode = ":" + keyCode;
            examineEvent.PushMarkup(Loc.GetString(channelFTLPattern,
                ("color", proto.Color),
                ("key", keyCode),
                ("id", proto.LocalizedName),
                ("freq", proto.Frequency)));
        }
    }
}
