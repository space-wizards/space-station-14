using Content.Shared.Disposal.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Disposal.Tube
{
    /// <summary>
    /// Initializes a <see cref="DisposalRouterWindow"/> and updates it when new server messages are received.
    /// </summary>
    [UsedImplicitly]
    public sealed class DisposalRouterBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private DisposalRouterWindow? _window;

        public DisposalRouterBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = this.CreateWindow<DisposalRouterWindow>();

            _window.Confirm.OnPressed += _ => ButtonPressed(DisposalRouterUiAction.Ok, _window.TagInput.Text);
            _window.TagInput.OnTextEntered += args => ButtonPressed(DisposalRouterUiAction.Ok, args.Text);
        }

        private void ButtonPressed(DisposalRouterUiAction action, string tag)
        {
            SendMessage(new DisposalRouterUiActionMessage(action, tag));
            Close();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (state is not DisposalRouterUserInterfaceState cast)
            {
                return;
            }

            _window?.UpdateState(cast);
        }
    }
}
