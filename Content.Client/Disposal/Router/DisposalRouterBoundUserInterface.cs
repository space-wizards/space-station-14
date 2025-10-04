using Content.Shared.Conduit.Tagger;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Disposal.Router
{
    /// <summary>
    /// Initializes a <see cref="DisposalRouterWindow"/> and updates it when new server messages are received.
    /// </summary>
    [UsedImplicitly]
    public sealed class DisposalRouterBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private DisposalRouterWindow? _window;

        private const int MaxTagLength = 150;

        public DisposalRouterBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = this.CreateWindow<DisposalRouterWindow>();

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
