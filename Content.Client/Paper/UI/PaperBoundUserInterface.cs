using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;
using Content.Shared.Paper;
using Content.Shared.Signature;
using static Content.Shared.Paper.PaperComponent;

namespace Content.Client.Paper.UI;

[UsedImplicitly]
public sealed class PaperBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private PaperWindow? _window;

    public PaperBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<PaperWindow>();
        _window.OnSaved += InputOnTextEntered;

        if (EntMan.TryGetComponent<PaperComponent>(Owner, out var paper))
        {
            _window.MaxInputLength = paper.ContentSize;
        }
        if (EntMan.TryGetComponent<PaperVisualsComponent>(Owner, out var visuals))
        {
            _window.InitVisuals(Owner, visuals);
        }
        if (EntMan.TryGetComponent<SignatureComponent>(Owner, out var signature))
        {
            _window.SignatureContainer.Visible = true;

            if (signature.Data != null)
                _window.InitSignature(signature.Data);
        }

        _window.LoadSavedSignatureData += OnLoadSavedSignatureData;
    }

    private void OnLoadSavedSignatureData()
    {
        var ev = new ApplySavedSignature();
        SendPredictedMessage(ev);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is PaperBoundUserInterfaceState paperState)
            _window?.Populate(paperState);

        if (_window == null)
            return;

        switch (state)
        {
            case UpdatePenBrushPaperState brushState:
                _window.UpdateBrush(brushState.BrushWriteSize, brushState.BrushEraseSize);
                break;
            case UpdateSignatureDataState signatureState:
                _window.Signature.SetSignature(signatureState.Data);
                break;
        }
    }

    private void InputOnTextEntered(string text)
    {
        SendMessage(new PaperInputTextMessage(text));

        if (_window != null)
        {
            _window.Input.TextRope = Rope.Leaf.Empty;
            _window.Input.CursorPosition = new TextEdit.CursorPos(0, TextEdit.LineBreakBias.Top);

            if (_window.Signature.Data != null)
                SendMessage(new SignatureSubmitMessage(_window.Signature.Data));
        }
    }
}
