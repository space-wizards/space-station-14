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
            args.PushMarkup(Loc.GetString("examine-encryption-key"));
            foreach (var id in component.Channels)
            {
                string ftlPattern = "examine-encryption-key-channel";
                if (id == "Common")
                    ftlPattern = "examine-encryption-key-common-channel";
                var proto = _protoManager.Index<RadioChannelPrototype>(id);
                args.PushMarkup(Loc.GetString(ftlPattern,
                    ("color", proto.Color),
                    ("key", proto.KeyCode),
                    ("id", proto.LocalizedName),
                    ("freq", proto.Frequency)));
            }
        }
    }
}
