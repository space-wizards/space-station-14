using Content.Shared.Examine;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;

namespace Content.Server.Headset
{
    public sealed class EncryptionChipSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _protoManager = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<EncryptionChipComponent, ExaminedEvent>(OnExamined);
        }
        private void OnExamined(EntityUid uid, EncryptionChipComponent component, ExaminedEvent args)
        {
            if (!args.IsInDetailsRange)
                return;
            // args.PushMarkup(Loc.GetString("examine-radio-frequency", ("frequency", component.BroadcastFrequency)));
            args.PushMarkup(Loc.GetString("examine-encryption-chip"));

            foreach (var id in component.Channels)
            {
                if (id == "Common")
                    continue;

                var proto = _protoManager.Index<RadioChannelPrototype>(id);
                args.PushMarkup(Loc.GetString("examine-headset-channel",
                    ("color", proto.Color),
                    ("key", proto.KeyCode),
                    ("id", proto.LocalizedName),
                    ("freq", proto.Frequency)));
            }

            args.PushMarkup(Loc.GetString("examine-headset-chat-prefix", ("prefix", ";")));
        }
    }
}
