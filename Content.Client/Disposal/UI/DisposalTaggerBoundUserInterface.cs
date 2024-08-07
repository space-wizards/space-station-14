using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using static Content.Shared.Disposal.Components.SharedDisposalTaggerComponent;

namespace Content.Client.Disposal.UI
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

            _window.Confirm.OnPressed += _ => ButtonPressed(UiAction.Ok, _window.TagInput.Text);
            _window.TagInput.OnTextEntered += args => ButtonPressed(UiAction.Ok, args.Text);
        }

        private void ButtonPressed(UiAction action, string tag)
        {
            // TODO: This looks copy-pasted with the other mailing stuff...
            SendMessage(new UiActionMessage(action, tag));
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
