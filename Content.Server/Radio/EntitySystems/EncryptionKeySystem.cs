using Content.Server.Radio.Components;
using Content.Shared.Examine;
using Content.Shared.Radio;
using Robust.Shared.Profiling;
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
            HeadsetSystem.GetChannelsExamine(component.Channels, args, _protoManager, "examine-headset-channel");
            if (component.DefaultChannel != null)
            {
                var proto = _protoManager.Index<RadioChannelPrototype>(component.DefaultChannel);
                args.PushMarkup(Loc.GetString("examine-encryption-key-default-channel", ("channel", component.DefaultChannel), ("color", proto.Color)));
            }
        }
    }
}
