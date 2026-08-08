using Content.Shared.Disposal.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using System.Collections.Generic;

namespace Content.Client.Disposal.Router
{
    /// <summary>
    /// Initializes a <see cref="DisposalRouterWindow"/> and updates it when new server messages are received.
    /// </summary>
    [UsedImplicitly]
    public sealed class DisposalRouterBoundUserInterface : BoundUserInterface
    {
        private DisposalRouterWindow? _window;

        private const int TagLimit = 150;

        public DisposalRouterBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = this.CreateWindow<DisposalRouterWindow>();

            _window.Confirm.OnPressed += _ => AcceptButtonPressed(_window.TagInput.Text);
            _window.TagInput.OnTextEntered += args => AcceptButtonPressed(args.Text);

            if (EntMan.TryGetComponent<DisposalRouterComponent>(Owner, out var router) &&
                router.Tags.Count > 0)
            {
                _window.TagInput.Text = string.Join(",", router.Tags);
            }
        }

        private void AcceptButtonPressed(string tag)
        {
            SendMessage(new DisposalRouterUiActionMessage(tag, TagLimit));
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
