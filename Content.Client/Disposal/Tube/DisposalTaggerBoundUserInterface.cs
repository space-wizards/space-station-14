using Content.Shared.Disposal.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Disposal.Tube
{
    /// <summary>
    /// Initializes a <see cref="DisposalTaggerWindow"/> and updates it when new server messages are received.
    /// </summary>
    [UsedImplicitly]
    public sealed class DisposalTaggerBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private DisposalTaggerWindow? _window;

        public DisposalTaggerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = this.CreateWindow<DisposalTaggerWindow>();

            _window.Confirm.OnPressed += _ => ButtonPressed(DisposalTaggerUiAction.Ok, _window.TagInput.Text);
            _window.TagInput.OnTextEntered += args => ButtonPressed(DisposalTaggerUiAction.Ok, args.Text);
        }

        private void ButtonPressed(DisposalTaggerUiAction action, string tag)
        {
            // TODO: This looks copy-pasted with the other mailing stuff...
            SendMessage(new DisposalTaggerUiActionMessage(action, tag));
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
