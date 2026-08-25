using Content.Shared.Disposal.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Disposal.Tagger
{
    /// <summary>
    /// Initializes a <see cref="DisposalTaggerWindow"/> and updates it when new server messages are received.
    /// </summary>
    [UsedImplicitly]
    public sealed class DisposalTaggerBoundUserInterface : BoundUserInterface
    {
        private DisposalTaggerWindow? _window;

        private const int TagLimit = 30;

        public DisposalTaggerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = this.CreateWindow<DisposalTaggerWindow>();

            _window.Confirm.OnPressed += _ => AcceptButtonPressed(_window.TagInput.Text);
            _window.TagInput.OnTextEntered += args => AcceptButtonPressed(args.Text);

            Update();
        }

        private void AcceptButtonPressed(string tag)
        {
            SendMessage(new DisposalTaggerUiActionMessage(tag, TagLimit));
            Close();
        }

        public override void Update()
        {
            base.Update();

            if (_window == null || !EntMan.TryGetComponent<DisposalTaggerComponent>(Owner, out var tagger))
                return;

            _window.TagInput.Text = tagger.Tag;
            _window.TagInput.Editable = tagger.Editable;

            _window.Confirm.Disabled = !tagger.Editable;
            _window.Confirm.Text =
                tagger.Editable ? Loc.GetString("generic-confirm") : Loc.GetString("generic-disabled");
        }
    }
}
