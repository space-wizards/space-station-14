using Content.Client.UserInterface.Systems.TextPrompt;
using Content.Shared.Labels;
using Content.Shared.Labels.Components;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client.Labels.UI
{
    /// <summary>
    /// Initializes a <see cref="HandLabelerWindow"/> and updates it when new server messages are received.
    /// </summary>
    public sealed partial class HandLabelerBoundUserInterface : BoundUserInterface
    {
        [Dependency] private IEntityManager _entManager = default!;

        [ViewVariables]
        private TextPromptWindow? _window;

        public HandLabelerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
            IoCManager.InjectDependencies(this);
        }

        protected override void Open()
        {
            base.Open();

            _window = this.CreateWindow<TextPromptWindow>();

            _window.UpdateLabels("hand-labeler-ui-header", "hand-labeler-ui-reset-label-text", "hand-labeler-ui-clear-label-text", "hand-labeler-current-text-label");

            if (_entManager.TryGetComponent(Owner, out HandLabelerComponent? labeler))
            {
                _window.SetMaxLabelLength(labeler!.MaxLabelChars);
            }

            _window.OnLabelChanged += OnLabelChanged;
            Reload();
            _window.SetInitialLabelState(); // Must be after Reload() has set the label text
        }

        private void OnLabelChanged(string newLabel)
        {
            // Focus moment
            if (_entManager.TryGetComponent(Owner, out HandLabelerComponent? labeler) &&
                labeler.AssignedLabel.Equals(newLabel))
                return;

            SendPredictedMessage(new HandLabelerLabelChangedMessage(newLabel));
        }

        public void Reload()
        {
            if (_window == null || !_entManager.TryGetComponent(Owner, out HandLabelerComponent? component))
                return;

            _window.SetCurrentLabel(component.AssignedLabel);
        }
    }
}
