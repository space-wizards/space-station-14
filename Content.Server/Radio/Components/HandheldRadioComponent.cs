using Content.Server.Radio.EntitySystems;
using Content.Server.RadioKey.Components;
using Content.Shared.Interaction;
using Content.Shared.Radio;

namespace Content.Server.Radio.Components
{
    [RegisterComponent]
    [ComponentProtoName("Radio")]
    [ComponentReference(typeof(IRadio))]
    [ComponentReference(typeof(IListen))]
    public sealed class HandheldRadioComponent : Component, IListen, IRadio
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        private RadioSystem _radioSystem = default!;
        private SharedRadioSystem _sharedRadioSystem = default!;

        [ViewVariables(VVAccess.ReadWrite)] [DataField("frequency")]
        public int Frequency = 1459;

        [ViewVariables(VVAccess.ReadWrite)] [DataField("listenRange")]
        public int ListenRange { get; private set; } = 7;

        // handheld radios and wall radios are slightly more configurable than headsets (UI CONFIGURABLE)
        [ViewVariables(VVAccess.ReadWrite)] [DataField("sendOn")]
        public bool Send;

        [ViewVariables(VVAccess.ReadWrite)] [DataField("receiveOn")]
        public bool Receive;

        protected override void Initialize()
        {
            base.Initialize();

            _radioSystem = EntitySystem.Get<RadioSystem>();
            _sharedRadioSystem = EntitySystem.Get<SharedRadioSystem>();
        }

        public bool CanListen(string message, EntityUid source, int? freq)
        {
            // iirc radios have one radiokey but its only being used for nukeop borgs for some reason. Didnt implement that here.
            return Send &&
                   EntitySystem.Get<SharedInteractionSystem>().InRangeUnobstructed(Owner, source, range: ListenRange);
        }

        public void Listen(MessagePacket message)
        {
            Broadcast(message);
        }

        public void Broadcast(MessagePacket message)
        {
            var channel = message.Channel ?? Frequency;

            if (_sharedRadioSystem.IsOutsideFreeFreq(channel))
            {
                if (_entMan.TryGetComponent<RadioKeyComponent>(Owner, out var comp))
                {
                    if (!comp.UnlockedFrequency.Contains(channel) || (message.Channel == 1213 && comp is { Syndie: false }))
                    {
                        message.Channel = Frequency;
                    }
                }
                else
                {
                    message.Channel = Frequency;
                }
            }
            _radioSystem.SpreadMessage(message);
        }

    }
}
