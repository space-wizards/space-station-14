using System.Linq;
using Content.Server.Store.Systems;
using Content.Shared.PDA;
using Content.Shared.PDA.Ringer;
using Content.Shared.Popups;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Server.Audio;

namespace Content.Server.PDA.Ringer
{
    public sealed class RingerSystem : SharedRingerSystem
    {
        [Dependency] private readonly PdaSystem _pda = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly UserInterfaceSystem _ui = default!;
        [Dependency] private readonly AudioSystem _audio = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly TransformSystem _xform = default!;

        public override void Initialize()
        {
            base.Initialize();

            // General Event Subscriptions
            SubscribeLocalEvent<RingerComponent, MapInitEvent>(RandomizeRingtone);
            SubscribeLocalEvent<RingerUplinkComponent, ComponentInit>(RandomizeUplinkCode);

            // RingerBoundUserInterface Subscriptions
            SubscribeLocalEvent<RingerComponent, RingerSetRingtoneMessage>(OnSetRingtone);
            SubscribeLocalEvent<RingerUplinkComponent, BeforeRingtoneSetEvent>(OnSetUplinkRingtone);
            SubscribeLocalEvent<RingerComponent, RingerPlayRingtoneMessage>(RingerPlayRingtone);
            SubscribeLocalEvent<RingerComponent, RingerRequestUpdateInterfaceMessage>(UpdateRingerUserInterfaceDriver);

            SubscribeLocalEvent<RingerComponent, CurrencyInsertAttemptEvent>(OnCurrencyInsert);
        }

        private void OnCurrencyInsert(EntityUid uid, RingerComponent ringer, CurrencyInsertAttemptEvent args)
        {
            if (!TryComp<RingerUplinkComponent>(uid, out var uplink))
            {
                args.Cancel();
                return;
            }

            // if the store can be locked, it must be unlocked first before inserting currency. Stops traitor checking.
            if (!uplink.Unlocked)
                args.Cancel();
        }

        private void RingerPlayRingtone(EntityUid uid, RingerComponent ringer, RingerPlayRingtoneMessage args)
        {
            StartRingtone(uid, ringer);
        }

        public void RingerPlayRingtone(Entity<RingerComponent?> ent)
        {
            if (!Resolve(ent, ref ent.Comp))
                return;

            StartRingtone(ent, ent.Comp);
        }

        private void StartRingtone(EntityUid uid, RingerComponent ringer)
        {
            ringer.Active = true;
            ringer.NoteCount = 0;
            ringer.NextNoteTime = _timing.CurTime;

            _popup.PopupEntity(Loc.GetString("comp-ringer-vibration-popup"), uid, Filter.Pvs(uid, 0.05f), false, PopupType.Medium);

            Dirty(uid, ringer);
            UpdateRingerUserInterface(uid, ringer, true);
        }

        private void UpdateRingerUserInterfaceDriver(EntityUid uid, RingerComponent ringer, RingerRequestUpdateInterfaceMessage args)
        {
            UpdateRingerUserInterface(uid, ringer, ringer.Active);
        }

        private void OnSetRingtone(EntityUid uid, RingerComponent ringer, RingerSetRingtoneMessage args)
        {
            // Prevent ringtone spam by checking the last time this ringtone was set
            var curTime = _timing.CurTime;
            if (ringer.LastRingtoneSetTime > curTime - TimeSpan.FromMilliseconds(250))
                return;

            ringer.LastRingtoneSetTime = curTime;

            // Client sent us an updated ringtone so set it to that.
            if (args.Ringtone.Length != RingtoneLength)
                return;

            var ev = new BeforeRingtoneSetEvent(args.Ringtone);
            RaiseLocalEvent(uid, ref ev);
            if (ev.Handled)
                return;

            UpdateRingerRingtone(uid, ringer, args.Ringtone);
        }

        private void OnSetUplinkRingtone(EntityUid uid, RingerUplinkComponent uplink, ref BeforeRingtoneSetEvent args)
        {
            if (uplink.Code.SequenceEqual(args.Ringtone) && HasComp<StoreComponent>(uid))
            {
                uplink.Unlocked = !uplink.Unlocked;
                if (TryComp<PdaComponent>(uid, out var pda))
                    _pda.UpdatePdaUi(uid, pda);

                // can't keep store open after locking it
                if (!uplink.Unlocked)
                    _ui.CloseUi(uid, StoreUiKey.Key);

                // no saving the code to prevent meta click set on sus guys pda -> wewlad
                args.Handled = true;
            }
        }

        /// <summary>
        /// Locks the uplink and closes the window, if its open
        /// </summary>
        /// <remarks>
        /// Will not update the PDA ui so you must do that yourself if needed
        /// </remarks>
        public void LockUplink(EntityUid uid, RingerUplinkComponent? uplink)
        {
            if (!Resolve(uid, ref uplink))
                return;

            uplink.Unlocked = false;
            _ui.CloseUi(uid, StoreUiKey.Key);
        }

        public void RandomizeRingtone(EntityUid uid, RingerComponent ringer, MapInitEvent args)
        {
            UpdateRingerRingtone(uid, ringer, GenerateRingtone());
        }

        public void RandomizeUplinkCode(EntityUid uid, RingerUplinkComponent uplink, ComponentInit args)
        {
            uplink.Code = GenerateRingtone();
        }

        //Non Event Functions

        private Note[] GenerateRingtone()
        {
            // Default to using C pentatonic so it at least sounds not terrible.
            return GenerateRingtone(new[]
            {
                Note.C,
                Note.D,
                Note.E,
                Note.G,
                Note.A
            });
        }

        private Note[] GenerateRingtone(Note[] notes)
        {
            var ringtone = new Note[RingtoneLength];

            for (var i = 0; i < RingtoneLength; i++)
            {
                ringtone[i] = _random.Pick(notes);
            }

            return ringtone;
        }

        private void UpdateRingerRingtone(EntityUid uid, RingerComponent ringer, Note[] ringtone)
        {
            // Assume validation has already happened.
            ringer.Ringtone = ringtone;
            Dirty(uid, ringer);
            UpdateRingerUserInterface(uid, ringer, ringer.Active);
        }

        private void UpdateRingerUserInterface(EntityUid uid, RingerComponent ringer, bool isPlaying)
        {
            _ui.SetUiState(uid, RingerUiKey.Key, new RingerUpdateState(isPlaying, ringer.Ringtone));
        }

        public bool ToggleRingerUI(EntityUid uid, EntityUid actor)
        {
            _ui.TryToggleUi(uid, RingerUiKey.Key, actor);
            return true;
        }

        public override void Update(float frameTime)
        {
            var curTime = _timing.CurTime;

            var ringerQuery = EntityQueryEnumerator<RingerComponent>();
            while (ringerQuery.MoveNext(out var uid, out var ringer))
            {
                if (!ringer.Active || !ringer.NextNoteTime.HasValue)
                    continue;

                // Check if it's time to play the next note
                if (curTime < ringer.NextNoteTime.Value)
                    continue;

                // Play current note
                var ringerXform = Transform(uid);
                _audio.PlayEntity(
                    GetSound(ringer.Ringtone[ringer.NoteCount]),
                    Filter.Empty().AddInRange(_xform.GetMapCoordinates(uid, ringerXform), ringer.Range),
                    uid,
                    true,
                    AudioParams.Default.WithMaxDistance(ringer.Range).WithVolume(ringer.Volume)
                );

                // Schedule next note
                ringer.NextNoteTime = curTime + TimeSpan.FromSeconds(NoteDelay);
                ringer.NoteCount++;

                // Check if we've finished playing all notes
                if (ringer.NoteCount >= RingtoneLength)
                {
                    ringer.Active = false;
                    ringer.NextNoteTime = null;
                    ringer.NoteCount = 0;
                    UpdateRingerUserInterface(uid, ringer, false);
                }
            }
        }

        private static SoundPathSpecifier GetSound(Note note)
        {
            return new SoundPathSpecifier($"/Audio/Effects/RingtoneNotes/{note.ToString().ToLower()}.ogg");
        }
    }

    [ByRefEvent]
    public record struct BeforeRingtoneSetEvent(Note[] Ringtone, bool Handled = false);
}
