using Content.Server.Radio.Components;
using Content.Server.Radio.EntitySystems;
using Content.Server.RadioKey.Components;
using Content.Shared.Radio;
namespace Content.Server.Headset
{
    [RegisterComponent]
    [ComponentReference(typeof(IRadio))]
    [ComponentReference(typeof(IListen))]
    public sealed class HeadsetComponent : Component, IListen, IRadio
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        private RadioSystem _radioSystem = default!;
        private SharedRadioSystem _sharedRadioSystem = default!;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("listenRange")]
        public int ListenRange { get; private set; }

        public bool RadioRequested { get; set; }

        [ViewVariables(VVAccess.ReadWrite)] [DataField("frequency")] public int Frequency = 1459;
        [ViewVariables(VVAccess.ReadWrite)] [DataField("command")] public bool Command = false;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("loudMode")]
        public bool LoudMode
        {
            get => _loudMode;
            set
            {
                // only allow on off if command is true
                _loudMode = Command ? value : false;
            }
        }

        private bool _loudMode = false;

        protected override void Initialize()
        {
            base.Initialize();

            _radioSystem = EntitySystem.Get<RadioSystem>();
            _sharedRadioSystem = EntitySystem.Get<SharedRadioSystem>();
        }

        public bool CanListen(string message, EntityUid source, int? chan)
        {
            return RadioRequested;
        }

        public void Listen(MessagePacket message)
        {
            Broadcast(message);
        }

        public void Broadcast(MessagePacket message)
        {
            var hasRKC = _entMan.TryGetComponent<RadioKeyComponent>(Owner, out var comp);
            var channel = message.Channel ?? Frequency;
            // wtf hasRKC is true so that means you got the component hello?
            if (_sharedRadioSystem.IsOutsideFreeFreq(channel) && hasRKC && comp != null && !comp.UnlockedFrequency.Contains(channel))
            {
                message.Channel = Frequency; // reset to common
            }

            _radioSystem.SpreadMessage(message);
            RadioRequested = false;
        }
    }
}
