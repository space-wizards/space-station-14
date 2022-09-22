using Content.Shared.Interaction;
using Content.Server.Radio.Components;
using Content.Shared.Radio;
using System.Text.RegularExpressions;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Radio.EntitySystems;
using System;

namespace Content.Server.Explosion.Components
{
    /// <summary>
    /// Sends a trigger when the keyphrase is heard
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(IListen))]
    public sealed class TriggerOnVoiceComponent : Component, IListen
    {
        public static readonly char[] PunctuationChars = new char[] { ',', '.', '?', '!', ';', ':' };

        private SharedInteractionSystem _sharedInteractionSystem = default!;
        private TriggerSystem _triggerSystem = default!;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("keyphrase")]
        public string KeyPhrase = "";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("listenRange")]
        public int ListenRange { get; private set; } = 4;

        /// <summary>
        /// Triggger enabled after first recording
        /// </summary>
        [ViewVariables]
        public bool Enabled = false;

        [ViewVariables]
        public bool IsRecording = false;

        [ViewVariables]
        public int MinLength = 3;

        [ViewVariables]
        public int MaxLength = 50;

        /// <summary>
        /// Displays 'recorded' popup only for the one who activated
        /// it in order to allow for stealthily recording others
        /// </summary>
        [ViewVariables]
        public EntityUid Activator;

        protected override void Initialize()
        {
            base.Initialize();

            _sharedInteractionSystem = EntitySystem.Get<SharedInteractionSystem>();
            _triggerSystem = EntitySystem.Get<TriggerSystem>();
        }

        bool IListen.CanListen(string message, EntityUid source, RadioChannelPrototype? channelPrototype)
        {
            //will hear standard speech and radio messages originating nearby but not independant whispers
            return _sharedInteractionSystem.InRangeUnobstructed(Owner, source, range: ListenRange);
        }

        void IListen.Listen(string message, EntityUid speaker, RadioChannelPrototype? channel)
        {
            message = message.Trim().TrimEnd(PunctuationChars);

            if (this.IsRecording && message.Length >= this.MinLength && message.Length <= this.MaxLength)
            {
                this.KeyPhrase = message;
                this.Enabled = true; //armed after first successful recording
                _triggerSystem.ToggleRecord(this, Activator, true);
            }
            else if (this.Enabled && Regex.IsMatch(message, $@"\b{Regex.Escape(KeyPhrase)}\b", RegexOptions.IgnoreCase))
            {
                _triggerSystem.Trigger(Owner, speaker);
            }
        }
    }
}
