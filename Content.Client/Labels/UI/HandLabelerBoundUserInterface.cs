using Content.Shared.Labels;
using Content.Shared.Labels.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Labels.UI
{
    /// <summary>
    /// Initializes a <see cref="HandLabelerWindow"/> and updates it when new server messages are received.
    /// </summary>
    public sealed class HandLabelerBoundUserInterface : BoundUserInterface
    {
        [Dependency] private readonly IEntityManager _entManager = default!;

        [ViewVariables]
        private HandLabelerWindow? _window;

        public HandLabelerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
            IoCManager.InjectDependencies(this);
        }

        protected override void Open()
        {
            _window = new HandLabelerWindow();
            _window.OpenCentered();

            _window.OnClose += Close;
            _window.OnLabelChanged += OnLabelChanged;

            base.Open();
        }

        private void OnLabelChanged(string newLabel)
        {
            if (_entManager.TryGetComponent(Owner, out HandLabelerComponent? labeler) &&
                labeler.AssignedLabel.Equals(newLabel))
            {
                return;
            }

            SendPredictedMessage(new HandLabelerLabelChangedMessage(newLabel));
        }

        public override void Refresh()
        {
            if (_window == null || !_entManager.TryGetComponent(Owner, out HandLabelerComponent? component))
                return;

            _window.SetCurrentLabel(component.AssignedLabel);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;
            _window?.Dispose();
        }
    }

}
