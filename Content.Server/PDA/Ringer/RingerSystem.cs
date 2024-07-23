using System.Linq;
using System.Runtime.InteropServices;
using Content.Server.Store.Components;
using Content.Server.Store.Systems;
using Content.Shared.PDA;
using Content.Shared.PDA.Ringer;
using Content.Shared.Popups;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Server.Audio;

namespace Content.Server.PDA.Ringer
{
    public sealed class RingerSystem : SharedRingerSystem
    {
        [Dependency] private readonly PdaSystem _pda = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly UserInterfaceSystem _ui = default!;
        [Dependency] private readonly AudioSystem _audio = default!;
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        [Dependency] private readonly TransformSystem _transform = default!;

        private readonly Dictionary<NetUserId, TimeSpan> _lastSetRingtoneAt = new();

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

        //Event Functions

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
            EnsureComp<ActiveRingerComponent>(uid);

            _popupSystem.PopupEntity(Loc.GetString("comp-ringer-vibration-popup"), uid, Filter.Pvs(uid, 0.05f), false, PopupType.Small);

            UpdateRingerUserInterface(uid, ringer, true);
        }

        public void RingerPlayRingtone(Entity<RingerComponent?> ent)
        {
            if (!Resolve(ent, ref ent.Comp))
                return;

            EnsureComp<ActiveRingerComponent>(ent);

            _popupSystem.PopupEntity(Loc.GetString("comp-ringer-vibration-popup"), ent, Filter.Pvs(ent, 0.05f), false, PopupType.Medium);

            UpdateRingerUserInterface(ent, ent.Comp, true);
        }

        private void UpdateRingerUserInterfaceDriver(EntityUid uid, RingerComponent ringer, RingerRequestUpdateInterfaceMessage args)
        {
            UpdateRingerUserInterface(uid, ringer, HasComp<ActiveRingerComponent>(uid));
        }

        private void OnSetRingtone(EntityUid uid, RingerComponent ringer, RingerSetRingtoneMessage args)
        {
            if (!TryComp(args.Actor, out ActorComponent? actorComp))
                return;

            ref var lastSetAt = ref CollectionsMarshal.GetValueRefOrAddDefault(_lastSetRingtoneAt, actorComp.PlayerSession.UserId, out var exists);

            // Delay on the client is 0.333, 0.25 is still enough and gives some leeway in case of small time differences
            if (exists && lastSetAt > _gameTiming.CurTime - TimeSpan.FromMilliseconds(250))
                return;

            lastSetAt = _gameTiming.CurTime;

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
            if (!Resolve(uid, ref uplink, true))
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

        private bool UpdateRingerRingtone(EntityUid uid, RingerComponent ringer, Note[] ringtone)
        {
            // Assume validation has already happened.
            ringer.Ringtone = ringtone;
            UpdateRingerUserInterface(uid, ringer, HasComp<ActiveRingerComponent>(uid));

            return true;
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

        public override void Update(float frameTime) //Responsible for actually playing the ringtone
        {
            var remove = new RemQueue<EntityUid>();

            var pdaQuery = EntityQueryEnumerator<RingerComponent, ActiveRingerComponent>();
            while (pdaQuery.MoveNext(out var uid, out var ringer, out var _))
            {
                ringer.TimeElapsed += frameTime;

                if (ringer.TimeElapsed < NoteDelay)
                    continue;

                ringer.TimeElapsed -= NoteDelay;
                var ringerXform = Transform(uid);

                _audio.PlayEntity(
                    GetSound(ringer.Ringtone[ringer.NoteCount]),
                    Filter.Empty().AddInRange(_transform.GetMapCoordinates(uid, ringerXform), ringer.Range),
                    uid,
                    true,
                    AudioParams.Default.WithMaxDistance(ringer.Range).WithVolume(ringer.Volume)
                );

                ringer.NoteCount++;

                if (ringer.NoteCount > RingtoneLength - 1)
                {
                    remove.Add(uid);
                    UpdateRingerUserInterface(uid, ringer, false);
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

        private static string GetSound(Note note)
        {
            return new ResPath("/Audio/Effects/RingtoneNotes/" + note.ToString().ToLower()) + ".ogg";
        }
    }

    [ByRefEvent]
    public record struct BeforeRingtoneSetEvent(Note[] Ringtone, bool Handled = false);
}
