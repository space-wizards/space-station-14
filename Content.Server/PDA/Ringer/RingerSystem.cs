using Content.Server.Store.Components;
using Content.Server.Store.Systems;
using Content.Server.UserInterface;
using Content.Shared.PDA;
using Content.Shared.PDA.Ringer;
using Content.Shared.Store;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Server.PDA.Ringer
{
    public sealed class RingerSystem : SharedRingerSystem
    {
        [Dependency] private readonly PDASystem _pda = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly StoreSystem _store = default!;
        [Dependency] private readonly UserInterfaceSystem _ui = default!;

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

        }

        //Event Functions

        private void RingerPlayRingtone(EntityUid uid, RingerComponent ringer, RingerPlayRingtoneMessage args)
        {
            EnsureComp<ActiveRingerComponent>(uid);
            UpdateRingerUserInterface(ringer);
        }

        private void UpdateRingerUserInterfaceDriver(EntityUid uid, RingerComponent ringer, RingerRequestUpdateInterfaceMessage args)
        {
            UpdateRingerUserInterface(ringer);
        }

        private void OnSetRingtone(EntityUid uid, RingerComponent ringer, RingerSetRingtoneMessage args)
        {
            // Client sent us an updated ringtone so set it to that.
            if (args.Ringtone.Length != RingtoneLength) return;

            var ev = new BeforeRingtoneSetEvent(args.Ringtone);
            RaiseLocalEvent(uid, ref ev);
            if (ev.Handled)
                return;

            UpdateRingerRingtone(ringer, args.Ringtone);
        }

        private void OnSetUplinkRingtone(EntityUid uid, RingerUplinkComponent uplink, ref BeforeRingtoneSetEvent args)
        {
            if (uplink.Code.SequenceEqual(args.Ringtone) && TryComp<StoreComponent>(uid, out var store))
            {
                uplink.Unlocked = !uplink.Unlocked;
                if (TryComp<PDAComponent>(uid, out var pda))
                    _pda.UpdatePdaUi(uid, pda);

                // can't keep store open after locking it
                if (!uplink.Unlocked)
                    _ui.TryCloseAll(uid, StoreUiKey.Key);

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
            if (!Resolve(uid, ref uplink, true))
                return;

            uplink.Unlocked = false;
            _ui.TryCloseAll(uid, StoreUiKey.Key);
        }

        public void RandomizeRingtone(EntityUid uid, RingerComponent ringer, MapInitEvent args)
        {
            UpdateRingerRingtone(ringer, GenerateRingtone());
        }

        public void RandomizeUplinkCode(EntityUid uid, RingerUplinkComponent uplink, ComponentInit args)
        {
            uplink.Code = GenerateRingtone();
        }

        //Non Event Functions

        private Note[] GenerateRingtone()
        {
            // Default to using C pentatonic so it at least sounds not terrible.
            var notes = new[]
            {
                Note.C,
                Note.D,
                Note.E,
                Note.G,
                Note.A,
            };

            var ringtone = new Note[RingtoneLength];

            for (var i = 0; i < 4; i++)
            {
                ringtone[i] = _random.Pick(notes);
            }

            return ringtone;
        }

        private bool UpdateRingerRingtone(RingerComponent ringer, Note[] ringtone)
        {
            // Assume validation has already happened.
            ringer.Ringtone = ringtone;
            UpdateRingerUserInterface(ringer);

            return true;
        }

        private void UpdateRingerUserInterface(RingerComponent ringer)
        {
            var ui = ringer.Owner.GetUIOrNull(RingerUiKey.Key);
            ui?.SetState(new RingerUpdateState(HasComp<ActiveRingerComponent>(ringer.Owner), ringer.Ringtone));
        }

        public bool ToggleRingerUI(RingerComponent ringer, IPlayerSession session)
        {
            var ui = ringer.Owner.GetUIOrNull(RingerUiKey.Key);
            ui?.Toggle(session);
            return true;
        }

        public override void Update(float frameTime) //Responsible for actually playing the ringtone
        {
            var remove = new RemQueue<EntityUid>();

            foreach(var (_, ringer) in EntityManager.EntityQuery<ActiveRingerComponent, RingerComponent>())
            {
                ringer.TimeElapsed += frameTime;

                if (ringer.TimeElapsed < NoteDelay) continue;

                ringer.TimeElapsed -= NoteDelay;
                var ringerXform = Transform(ringer.Owner);

                SoundSystem.Play(GetSound(ringer.Ringtone[ringer.NoteCount]),
                    Filter.Empty().AddInRange(ringerXform.MapPosition, ringer.Range),
                    ringer.Owner, AudioParams.Default.WithMaxDistance(ringer.Range).WithVolume(ringer.Volume));

                ringer.NoteCount++;

                if (ringer.NoteCount > 3)
                {
                    remove.Add(ringer.Owner);
                    UpdateRingerUserInterface(ringer);
                    ringer.TimeElapsed = 0;
                    ringer.NoteCount = 0;
                    break;
                }
            }

            foreach (var ent in remove)
            {
                RemComp<ActiveRingerComponent>(ent);
            }
        }

        private string GetSound(Note note)
        {
            return new ResPath("/Audio/Effects/RingtoneNotes/" + note.ToString().ToLower()) + ".ogg";
        }
    }
}

[ByRefEvent]
public record struct BeforeRingtoneSetEvent(Note[] Ringtone, bool Handled = false);
