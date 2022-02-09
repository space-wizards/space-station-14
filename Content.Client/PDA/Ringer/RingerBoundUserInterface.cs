using System;
using Content.Client.Message;
using Content.Shared.PDA;
using Content.Shared.PDA.Ringer;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
namespace Content.Client.PDA.Ringer
{
    [UsedImplicitly]
    public sealed class RingerBoundUserInterface : BoundUserInterface
    {
        private RingtoneMenu? _menu;

        public RingerBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();
            _menu = new RingtoneMenu();
            _menu.OpenToLeft();
            _menu.OnClose += Close;

            _menu.TestRingerButton.OnPressed += _ =>
            {
                SendMessage(new RingerPlayRingtoneMessage());
            };

            _menu.SetRingerButton.OnPressed += _ =>
            {
                if (!TryGetRingtone(out var ringtone)) return;

                SendMessage(new RingerSetRingtoneMessage(ringtone));
            };
        }

        private bool TryGetRingtone(out Note[] ringtone)
        {
            if (_menu == null)
            {
                ringtone = Array.Empty<Note>();
                return false;
            }

            ringtone = new Note[4];

            if (!Enum.TryParse<Note>(_menu.RingerNoteOneInput.Text.Replace("#", "sharp"), false, out var one)) return false;
            ringtone[0] = one;

            if (!Enum.TryParse<Note>(_menu.RingerNoteTwoInput.Text.Replace("#", "sharp"), false, out var two)) return false;
            ringtone[1] = two;

            if (!Enum.TryParse<Note>(_menu.RingerNoteThreeInput.Text.Replace("#", "sharp"), false, out var three)) return false;
            ringtone[2] = three;

            if (!Enum.TryParse<Note>(_menu.RingerNoteFourInput.Text.Replace("#", "sharp"), false, out var four)) return false;
            ringtone[3] = four;

            return true;
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (_menu == null)
            {
                return;
            }

            switch (state)
            {
                case RingerUpdateState msg:
                {
                    var noteOne = msg.Ringtone[0].ToString();
                    var noteTwo = msg.Ringtone[1].ToString();
                    var noteThree = msg.Ringtone[2].ToString();
                    var noteFour = msg.Ringtone[3].ToString();

                    if (RingtoneMenu.IsNote(noteOne))
                        _menu.RingerNoteOneInput.Text = noteOne.Replace("sharp", "#");

                    if (RingtoneMenu.IsNote(noteTwo))
                        _menu.RingerNoteTwoInput.Text = noteTwo.Replace("sharp", "#");

                    if (RingtoneMenu.IsNote(noteThree))
                        _menu.RingerNoteThreeInput.Text = noteThree.Replace("sharp", "#");

                    if (RingtoneMenu.IsNote(noteFour))
                        _menu.RingerNoteFourInput.Text = noteFour.Replace("sharp", "#");

                    _menu.TestRingerButton.Visible = !msg.IsPlaying;
                    break;
                }
            }
        }


        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            _menu?.Dispose();
        }
    }
}
