using Content.Server.Radio.EntitySystems;
using Content.Shared.Interaction;

namespace Content.Server.Radio.Components
{
    [RegisterComponent]
    [ComponentProtoName("Radio")]
    [ComponentReference(typeof(IRadio))]
    [ComponentReference(typeof(IListen))]
    public sealed class HandheldRadioComponent : Component, IListen, IRadio
    {
        private RadioSystem _radioSystem = default!;
        [ViewVariables(VVAccess.ReadWrite)] [DataField("frequency")] public int Frequency = 1459;

        [ViewVariables(VVAccess.ReadWrite)] [DataField("listenRange")] public int ListenRange { get; private set; } = 7;

        // handheld radios and wall radios are slightly more configurable than headsets (UI CONFIGURABLE)
        [ViewVariables(VVAccess.ReadWrite)]
        public bool TXOn
        {
            get => _txOn;
            private set
            {
                _txOn = value;
                Dirty();
            }
        }

        [DataField("sendOn")]
        private bool _txOn;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool RXOn
        {
            get => _rxOn;
            private set
            {
                _rxOn = value;
                Dirty();
            }
        }

        [DataField("receiveOn")]
        private bool _rxOn;

        protected override void Initialize()
        {
            base.Initialize();

            _radioSystem = EntitySystem.Get<RadioSystem>();

            TXOn = false;
            RXOn = false;
        }

        public bool CanListen(string message, EntityUid source, int? freq)
        {
            // iirc radios have one radiokey but its only being used for nukeop borgs for some reason. Didnt implement that here.
            return TXOn &&
                   EntitySystem.Get<SharedInteractionSystem>().InRangeUnobstructed(Owner, source, range: ListenRange);
        }

        public void Listen(MessagePacket message)
        {
            Broadcast(message);
        }

        public void Broadcast(MessagePacket message)
        {
            _radioSystem.SpreadMessage(message);
        }

    }
}
