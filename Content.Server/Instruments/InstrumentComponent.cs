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
    [ComponentReference(typeof(IActivate))]
    public class InstrumentComponent
        : SharedInstrumentComponent,
            IDropped,
            IHandSelected,
            IHandDeselected,
            IActivate,
            IUse,
            IThrown
    {
        private InstrumentSystem _instrumentSystem = default!;

        /// <summary>
        ///     The client channel currently playing the instrument, or null if there's none.
        /// </summary>
        [ViewVariables]
        private IPlayerSession? _instrumentPlayer;

        [DataField("handheld")]
        private bool _handheld;

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

        /// <summary>
        ///     Whether the instrument is an item which can be held or not.
        /// </summary>
        [ViewVariables]
        public bool Handheld => _handheld;

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

        public IPlayerSession? InstrumentPlayer
        {
            get => _instrumentPlayer;
            private set
            {
                Playing = false;

                if (_instrumentPlayer != null)
                    _instrumentPlayer.PlayerStatusChanged -= OnPlayerStatusChanged;

                _instrumentPlayer = value;

                if (value != null)
                    _instrumentPlayer!.PlayerStatusChanged += OnPlayerStatusChanged;
            }
        }

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(InstrumentUiKey.Key);

        private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
        {
            if (e.Session != _instrumentPlayer || e.NewStatus != SessionStatus.Disconnected) return;
            InstrumentPlayer = null;
            Clean();
        }

        protected override void Initialize()
        {
            base.Initialize();

            if (UserInterface != null)
            {
                UserInterface.OnClosed += UserInterfaceOnClosed;
            }

            _instrumentSystem = EntitySystem.Get<InstrumentSystem>();
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new InstrumentState(Playing, InstrumentProgram, InstrumentBank, AllowPercussion, AllowProgramChange, RespectMidiLimits, _lastSequencerTick);
        }

        public override void HandleNetworkMessage(ComponentMessage message, INetChannel channel, ICommonSession? session = null)
        {
            base.HandleNetworkMessage(message, channel, session);

            var maxMidiLaggedBatches = _instrumentSystem.MaxMidiLaggedBatches;
            var maxMidiEventsPerSecond = _instrumentSystem.MaxMidiEventsPerSecond;
            var maxMidiEventsPerBatch = _instrumentSystem.MaxMidiEventsPerBatch;

            switch (message)
            {
                case InstrumentMidiEventMessage midiEventMsg:
                    if (!Playing || session != _instrumentPlayer || InstrumentPlayer == null) return;

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
                        SendNetworkMessage(midiEventMsg);
                    }

                    var maxTick = midiEventMsg.MidiEvent.Max(x => x.Tick);
                    _lastSequencerTick = Math.Max(maxTick, minTick);
                    break;
                case InstrumentStartMidiMessage startMidi:
                    if (session != _instrumentPlayer)
                        break;
                    Playing = true;
                    break;
                case InstrumentStopMidiMessage stopMidi:
                    if (session != _instrumentPlayer)
                        break;
                    Playing = false;
                    Clean();
                    break;
            }
        }

        private void Clean()
        {
            Playing = false;
            _lastSequencerTick = 0;
            _batchesDropped = 0;
            _laggedBatches = 0;
        }

        void IDropped.Dropped(DroppedEventArgs eventArgs)
        {
            Clean();
            SendNetworkMessage(new InstrumentStopMidiMessage());
            InstrumentPlayer = null;
            UserInterface?.CloseAll();
        }

        void IThrown.Thrown(ThrownEventArgs eventArgs)
        {
            Clean();
            SendNetworkMessage(new InstrumentStopMidiMessage());
            InstrumentPlayer = null;
            UserInterface?.CloseAll();
        }

        void IHandSelected.HandSelected(HandSelectedEventArgs eventArgs)
        {
            if (eventArgs.User == null || !eventArgs.User.TryGetComponent(out ActorComponent? actor))
                return;

            var session = actor.PlayerSession;

            if (session.Status != SessionStatus.InGame) return;

            InstrumentPlayer = session;
        }

        void IHandDeselected.HandDeselected(HandDeselectedEventArgs eventArgs)
        {
            Clean();
            SendNetworkMessage(new InstrumentStopMidiMessage());
            UserInterface?.CloseAll();
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            if (Handheld)
                return;

            InteractInstrument(eventArgs.User);
        }

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            InteractInstrument(eventArgs.User);
            return false;
        }

        private void InteractInstrument(IEntity user)
        {
            if (!user.TryGetComponent(out ActorComponent? actor)) return;

            if ((!Handheld && InstrumentPlayer != null)
                || (Handheld && actor.PlayerSession != InstrumentPlayer)
                || !EntitySystem.Get<ActionBlockerSystem>().CanInteract(user)) return;

            InstrumentPlayer = actor.PlayerSession;
            OpenUserInterface(InstrumentPlayer);

            return;
        }

        private void UserInterfaceOnClosed(IPlayerSession player)
        {
            if (Handheld || player != InstrumentPlayer) return;

            Clean();
            InstrumentPlayer = null;
            SendNetworkMessage(new InstrumentStopMidiMessage());
        }

        private void OpenUserInterface(IPlayerSession session)
        {
            UserInterface?.Toggle(session);
        }

        public override void Update(float delta)
        {
            base.Update(delta);

            var maxMidiLaggedBatches = _instrumentSystem.MaxMidiLaggedBatches;
            var maxMidiBatchDropped = _instrumentSystem.MaxMidiBatchesDropped;

            if (_instrumentPlayer != null
                && (_instrumentPlayer.AttachedEntity == null
                    || !EntitySystem.Get<ActionBlockerSystem>().CanInteract(_instrumentPlayer.AttachedEntity)))
            {
                InstrumentPlayer = null;
                Clean();
                UserInterface?.CloseAll();
            }

            if ((_batchesDropped >= maxMidiBatchDropped
                    || _laggedBatches >= maxMidiLaggedBatches)
                && InstrumentPlayer != null && _respectMidiLimits)
            {
                var mob = InstrumentPlayer.AttachedEntity;

                SendNetworkMessage(new InstrumentStopMidiMessage());
                Playing = false;

                UserInterface?.CloseAll();

                if (mob != null)
                {
                    EntitySystem.Get<StunSystem>().TryParalyze(mob.Uid, TimeSpan.FromSeconds(1));
                    Clean();

                    Owner.PopupMessage(mob, "instrument-component-finger-cramps-max-message");
                }

                InstrumentPlayer = null;
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
