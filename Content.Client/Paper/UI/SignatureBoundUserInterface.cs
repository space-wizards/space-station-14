using Content.Client.Labels.UI;
using Content.Client.UserInterface.Systems.TextPrompt;
using Content.Shared.Labels;
using Content.Shared.Labels.Components;
using Content.Shared.Paper;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client.Paper.UI
{
    /// <summary>
    /// Initializes a <see cref="SignatureWindow"/> and updates it when new server messages are received
    /// </summary>
    public sealed partial class SignatureBoundUserInterface : BoundUserInterface
    {
        [Dependency] private IEntityManager _entManager = default!;

        [ViewVariables]
        private TextPromptWindow? _window;

        public SignatureBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
            IoCManager.InjectDependencies(this);
        }

        protected override void Open()
        {
            base.Open();

            _window = this.CreateWindow<TextPromptWindow>();
            _window.UpdateLabels("signature-ui-header", "signature-ui-reset-label-text", "signature-ui-clear-label-text", "signature-ui-current-text-label");

            if (_entManager.TryGetComponent(Owner, out PaperSignerComponent? signer))
            {
                _window.SetMaxLabelLength(signer!.MaxSignatureLength);
            }

            _window.OnLabelChanged += OnSignatureChanged;
            Reload();
            _window.SetInitialLabelState(); // Must be after Reload() has set the signature text
        }

        private void OnSignatureChanged(string newSignature)
        {
            // Focus moment
            if (_entManager.TryGetComponent(Owner, out PaperSignerComponent? signer) &&
                signer.Signature.Equals(newSignature))
                return;

            SendPredictedMessage(new SignatureChangedMessage(newSignature));
        }

        public void Reload()
        {
            if (_window == null || !_entManager.TryGetComponent(Owner, out PaperSignerComponent? component))
                return;

            _window.SetCurrentLabel(component.Signature);
        }
    }
}
