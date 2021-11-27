using System;
using System.Linq;
using Content.Server.Stunnable;
using Content.Server.Stunnable.Components;
using Content.Server.UserInterface;
using Content.Shared.ActionBlocker;
using Content.Shared.Hands;
using Content.Shared.Instruments;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Network;
using Robust.Shared.Players;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Instruments
{

    [RegisterComponent]
    public class InstrumentComponent
        : SharedInstrumentComponent
    {
        private InstrumentSystem _instrumentSystem = default!;

        [ViewVariables]
        private bool _playing = false;

        [ViewVariables]
        private float _timer = 0f;

        [ViewVariables]
        private int _batchesDropped = 0;

        [ViewVariables]
        private int _laggedBatches = 0;

        [ViewVariables]
        private uint _lastSequencerTick = 0;

        [ViewVariables]
        private int _midiEventCount = 0;

        [DataField("program")]
        private byte _instrumentProgram = 1;
        [DataField("bank")]
        private byte _instrumentBank;
        [DataField("allowPercussion")]
        private bool _allowPercussion;
        [DataField("allowProgramChange")]
        private bool _allowProgramChange;
        [DataField("respectMidiLimits")]
        private bool _respectMidiLimits = true;

        public override byte InstrumentProgram { get => _instrumentProgram;
            set
            {
                _instrumentProgram = value;
                Dirty();
            }
        }

        public override byte InstrumentBank { get => _instrumentBank;
            set
            {
                _instrumentBank = value;
                Dirty();
            }
        }

        public override bool AllowPercussion { get => _allowPercussion;
            set
            {
                _allowPercussion = value;
                Dirty();
            }
        }

        public override bool AllowProgramChange { get => _allowProgramChange;
            set
            {
                _allowProgramChange = value;
                Dirty();
            }
        }

        public override bool RespectMidiLimits { get => _respectMidiLimits;
            set
            {
                _respectMidiLimits = value;
                Dirty();
            }
        }

        public IPlayerSession? InstrumentPlayer => Owner.GetComponentOrNull<ActivatableUIComponent>()?.CurrentSingleUser;

        /// <summary>
        ///     Whether the instrument is currently playing or not.
        /// </summary>
        [ViewVariables]
        public bool Playing
        {
            get => _playing;
            set
            {
                _playing = value;
                Dirty();
            }
        }

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(InstrumentUiKey.Key);

        protected override void Initialize()
        {
            base.Initialize();

            _instrumentSystem = EntitySystem.Get<InstrumentSystem>();
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new InstrumentState(Playing, InstrumentProgram, InstrumentBank, AllowPercussion, AllowProgramChange, RespectMidiLimits, _lastSequencerTick);
        }

        [Obsolete("Component Messages are deprecated, use Entity Events instead.")]
        public override void HandleNetworkMessage(ComponentMessage message, INetChannel channel, ICommonSession? session = null)
        {
            base.HandleNetworkMessage(message, channel, session);

            var maxMidiLaggedBatches = _instrumentSystem.MaxMidiLaggedBatches;
            var maxMidiEventsPerSecond = _instrumentSystem.MaxMidiEventsPerSecond;
            var maxMidiEventsPerBatch = _instrumentSystem.MaxMidiEventsPerBatch;

            switch (message)
            {
                case InstrumentMidiEventMessage midiEventMsg:
                    if (!Playing || session != InstrumentPlayer || InstrumentPlayer == null) return;

                    var send = true;

                    var minTick = midiEventMsg.MidiEvent.Min(x => x.Tick);
                    if (_lastSequencerTick > minTick)
                    {
                        _laggedBatches++;

                        if (_respectMidiLimits)
                        {
                            if (_laggedBatches == (int) (maxMidiLaggedBatches * (1 / 3d) + 1))
                            {
                                InstrumentPlayer.AttachedEntity?.PopupMessage(
                                    Loc.GetString("instrument-component-finger-cramps-light-message"));
                            } else if (_laggedBatches == (int) (maxMidiLaggedBatches * (2 / 3d) + 1))
                            {
                                InstrumentPlayer.AttachedEntity?.PopupMessage(
                                    Loc.GetString("instrument-component-finger-cramps-serious-message"));
                            }
                        }

                        if (_laggedBatches > maxMidiLaggedBatches)
                        {
                            send = false;
                        }
                    }

                    if (++_midiEventCount > maxMidiEventsPerSecond
                        || midiEventMsg.MidiEvent.Length > maxMidiEventsPerBatch)
                    {
                        _batchesDropped++;

                        send = false;
                    }

                    if (send || !_respectMidiLimits)
                    {
#pragma warning disable 618
                        SendNetworkMessage(midiEventMsg);
#pragma warning restore 618
                    }

                    var maxTick = midiEventMsg.MidiEvent.Max(x => x.Tick);
                    _lastSequencerTick = Math.Max(maxTick, minTick);
                    break;
                case InstrumentStartMidiMessage startMidi:
                    if (session != InstrumentPlayer)
                        break;
                    Playing = true;
                    break;
                case InstrumentStopMidiMessage stopMidi:
                    if (session != InstrumentPlayer)
                        break;
                    Playing = false;
                    Clean();
                    break;
            }
        }

        public void Clean()
        {
            if (Playing)
            {
#pragma warning disable 618
                SendNetworkMessage(new InstrumentStopMidiMessage());
#pragma warning restore 618
            }
            Playing = false;
            _lastSequencerTick = 0;
            _batchesDropped = 0;
            _laggedBatches = 0;
        }

        public override void Update(float delta)
        {
            base.Update(delta);

            var maxMidiLaggedBatches = _instrumentSystem.MaxMidiLaggedBatches;
            var maxMidiBatchDropped = _instrumentSystem.MaxMidiBatchesDropped;

            if ((_batchesDropped >= maxMidiBatchDropped
                    || _laggedBatches >= maxMidiLaggedBatches)
                && InstrumentPlayer != null && _respectMidiLimits)
            {
                var mob = InstrumentPlayer.AttachedEntity;

                // Just in case
                Clean();
                UserInterface?.CloseAll();

                if (mob != null)
                {
                    EntitySystem.Get<StunSystem>().TryParalyze(mob.Uid, TimeSpan.FromSeconds(1));

                    Owner.PopupMessage(mob, "instrument-component-finger-cramps-max-message");
                }
            }

            _timer += delta;
            if (_timer < 1) return;

            _timer = 0f;
            _midiEventCount = 0;
            _laggedBatches = 0;
            _batchesDropped = 0;
        }

    }

}
