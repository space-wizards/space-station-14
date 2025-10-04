using Content.Shared.Conduit.Tagger;
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
        [ViewVariables]
        private DisposalTaggerWindow? _window;

        private const int MaxTagLength = 30;

        public DisposalTaggerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = this.CreateWindow<DisposalTaggerWindow>();

            _window.Confirm.OnPressed += _ => ButtonPressed(ConduitTaggerUiAction.Ok, _window.TagInput.Text);
            _window.TagInput.OnTextEntered += args => ButtonPressed(ConduitTaggerUiAction.Ok, args.Text);
        }

        private void ButtonPressed(ConduitTaggerUiAction action, string tag)
        {
            SendMessage(new ConduitTaggerUiActionMessage(action, tag, MaxTagLength));
            Close();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (state is not ConduitTaggerUserInterfaceState cast)
                return;

            _window?.UpdateState(cast);
        }
    }
}
