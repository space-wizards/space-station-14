using System.Linq;
using Content.Server.Radio.EntitySystems;
using Content.Shared.Radio;

namespace Content.Server.Radio.Components.Telecomms;

[RegisterComponent]
[ComponentReference(typeof(TelecommsMachine))]
[ComponentReference(typeof(ITelecommsFrequencyChanger))]
[ComponentReference(typeof(ITelecommsFrequencyFilter))]
[ComponentReference(typeof(ITelecommsLogger))]
public sealed class TelecommsAllInOneComponent : TelecommsMachine, ITelecommsFrequencyChanger, ITelecommsFrequencyFilter, ITelecommsLogger
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("listeningFrequency")]
        public List<int> ListeningFrequency { get; set; } = new();

        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("listeningFrequencyRange")]
        private List<int> _listeningFrequencyRange = new();

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("frequencyToChange")]
        public int? FrequencyToChange { get; set; } = null;

        [ViewVariables(VVAccess.ReadWrite)]
        public List<TelecommsLog> TelecommsLog { get; set; } = new();

        protected override void Initialize()
        {
            base.Initialize();
            var srs = EntitySystem.Get<SharedRadioSystem>();
            if (_listeningFrequencyRange.Count != 2 ||
                _listeningFrequencyRange.First() >= _listeningFrequencyRange.Last()) return;
            for (var i = _listeningFrequencyRange.First(); i < _listeningFrequencyRange.Last(); i += 2)
            {
                var sanitizedFrequency = srs.SanitizeFrequency(i, true);
                if (ListeningFrequency.Contains(sanitizedFrequency)) continue;
                ListeningFrequency.Add(sanitizedFrequency);
            }
        }

        public bool IsFrequencyListening(int freq)
        {
            return ListeningFrequency.Count == 0 || ListeningFrequency.Contains(freq);
        }
    }
