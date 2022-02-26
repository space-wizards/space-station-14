using Content.Server.UserInterface;
using Content.Shared.PDA.Ringer;
using Robust.Shared.GameObjects;
using Robust.Shared.Audio;
using Robust.Server.Player;
using Robust.Shared.Player;
using System;
using System.Collections.Generic;
using Content.Shared.Audio;
using Content.Shared.PDA;
using Content.Shared.Sound;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.PDA.Ringer
{
    public sealed class RingerSystem : SharedRingerSystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;

        public override void Initialize()
        {
            base.Initialize();

            // General Event Subscriptions
            SubscribeLocalEvent<RingerComponent, ComponentInit>(RandomizeRingtone);
            // RingerBoundUserInterface Subscriptions
            SubscribeLocalEvent<RingerComponent, RingerSetRingtoneMessage>(OnSetRingtone);
            SubscribeLocalEvent<RingerComponent, RingerPlayRingtoneMessage>(RingerPlayRingtone);
            SubscribeLocalEvent<RingerComponent, RingerRequestUpdateInterfaceMessage>(UpdateRingerUserInterfaceDriver);
        }

        //Event Functions

        private void RingerPlayRingtone(EntityUid uid, RingerComponent ringer, RingerPlayRingtoneMessage args)
        {
            ringer.IsPlaying = true;
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

            UpdateRingerRingtone(ringer, args.Ringtone);
        }

        public void RandomizeRingtone(EntityUid uid, RingerComponent ringer, ComponentInit args)
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

            UpdateRingerRingtone(ringer, ringtone);

        }

        //Non Event Functions

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
            ui?.SetState(new RingerUpdateState(ringer.IsPlaying, ringer.Ringtone));
        }

        public bool ToggleRingerUI(RingerComponent ringer, IPlayerSession session)
        {
            var ui = ringer.Owner.GetUIOrNull(RingerUiKey.Key);
            ui?.Toggle(session);
            return true;
        }

        public override void Update(float frameTime) //Responsible for actually playing the ringtone
        {
            foreach(var ringer in EntityManager.EntityQuery<RingerComponent>())
            {
                // If this is perf problem then something something custom tracking via hashset.
                if (!ringer.IsPlaying)
                    continue;

                ringer.TimeElapsed += frameTime;

                if (ringer.TimeElapsed < NoteDelay) continue;

                ringer.TimeElapsed -= NoteDelay;
                var ringerXform = Transform(ringer.Owner);

                SoundSystem.Play(
                    Filter.Empty().AddInRange(ringerXform.MapPosition, ringer.Range),
                    GetSound(ringer.Ringtone[ringer.NoteCount]),
                    ringer.Owner,
                    AudioParams.Default.WithMaxDistance(ringer.Range).WithVolume(ringer.Volume));

                ringer.NoteCount++;

                if (ringer.NoteCount > 3)
                {
                    ringer.IsPlaying = false;
                    UpdateRingerUserInterface(ringer);
                    ringer.TimeElapsed = 0;
                    ringer.NoteCount = 0;
                    break;
                }
            }
        }

        private string GetSound(Note note)
        {
            return new ResourcePath("/Audio/Effects/RingtoneNotes/" + note.ToString().ToLower()) + ".ogg";
        }
    }
}
