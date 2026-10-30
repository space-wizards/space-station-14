using Content.Shared.Disposal.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Disposal.Tagger
{
    /// <summary>
    /// Initializes a <see cref="DisposalTaggerWindow"/> and updates it when new server messages are received.
    /// </summary>
    [UsedImplicitly]
    public sealed class DisposalTaggerBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
    {
        private DisposalTaggerWindow? _window;

        public const int TagLimit = 30;

        protected override void Open()
        {
            base.Open();

            _window = this.CreateWindow<DisposalTaggerWindow>();
            _window.OnRouteChanged += NewRoute;

            Update();
        }

        private void NewRoute(string route)
        {
            SendMessage(new DisposalTaggerUiActionMessage(route, TagLimit));
            Close();
        }

        public override void Update()
        {
            base.Update();

            if (_window == null || !EntMan.TryGetComponent<DisposalTaggerComponent>(Owner, out var tagger))
                return;

            _window.Populate(tagger.Tag, tagger.Editable);
        }
    }
}
