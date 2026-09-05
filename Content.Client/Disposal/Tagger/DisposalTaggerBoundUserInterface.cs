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

            if (EntMan.TryGetComponent<DisposalTaggerComponent>(Owner, out var tagger) &&
                tagger.Tag != string.Empty)
            {
                _window.TagInput.Text = tagger.Tag;
            }
        }

        private void AcceptButtonPressed(string tag)
        {
            SendMessage(new DisposalTaggerUiActionMessage(tag, TagLimit));
            Close();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (state is not DisposalTaggerUserInterfaceState cast)
            {
                return;
            }

            _window?.UpdateState(cast);
        }
    }
}
