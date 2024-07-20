using Content.Shared.PDA;
using Content.Shared.PDA.Ringer;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Timing;

namespace Content.Client.PDA.Ringer
{
    [UsedImplicitly]
    public sealed class RingerBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private RingtoneMenu? _menu;

        public RingerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();
            _menu = this.CreateWindow<RingtoneMenu>();
            _menu.OpenToLeft();

            _menu.TestRingerButton.OnPressed += _ =>
            {
                SendMessage(new RingerPlayRingtoneMessage());
            };

            _menu.SetRingerButton.OnPressed += _ =>
            {
                if (!TryGetRingtone(out var ringtone))
                    return;

                SendMessage(new RingerSetRingtoneMessage(ringtone));
                _menu.SetRingerButton.Disabled = true;

                Timer.Spawn(333, () =>
                {
                    if (_menu is { Disposed: false, SetRingerButton: { Disposed: false } ringer})
                        ringer.Disabled = false;
                });
            };
        }

        private bool TryGetRingtone(out Note[] ringtone)
        {
            if (_menu == null)
            {
                ringtone = Array.Empty<Note>();
                return false;
            }

            ringtone = new Note[_menu.RingerNoteInputs.Length];

            for (int i = 0; i < _menu.RingerNoteInputs.Length; i++)
            {
                if (!Enum.TryParse<Note>(_menu.RingerNoteInputs[i].Text.Replace("#", "sharp"), false, out var note))
                    return false;
                ringtone[i] = note;
            }

            return true;
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (_menu == null || state is not RingerUpdateState msg)
                return;

            for (int i = 0; i < _menu.RingerNoteInputs.Length; i++)
            {

                var note = msg.Ringtone[i].ToString();
                if (RingtoneMenu.IsNote(note))
                {
                    _menu.PreviousNoteInputs[i] = note.Replace("sharp", "#");
                    _menu.RingerNoteInputs[i].Text = _menu.PreviousNoteInputs[i];
                }

            }

            _menu.TestRingerButton.Disabled = msg.IsPlaying;
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
