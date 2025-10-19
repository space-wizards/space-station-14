using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Content.Shared.Paper;
using static Content.Shared.Paper.PaperComponent;

namespace Content.Client.Paper.UI;

[UsedImplicitly]
public sealed class PaperBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private PaperWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<PaperWindow>();
        _window.OnNewEdit += InputOnTextEntered;
        _window.OnFullEdit += InputOnFullTextEntered;

        if (EntMan.TryGetComponent<PaperComponent>(Owner, out var paper))
        {
            _window.SetMaxInputLength(paper.ContentSize);
        }

        if (EntMan.TryGetComponent<PaperVisualsComponent>(Owner, out var visuals))
        {
            _window.InitVisuals(Owner, visuals);
        }
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        base.ReceiveMessage(message);

        switch (message)
        {
            case PaperBeginFullEditMessage msg:
                _window?.EnableFullEditMode(msg.EditTool);
                break;
            case PaperBeginEditMessage msg:
                _window?.EnableEditMode(msg.EditTool);
                break;
        }
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        _window?.Populate((PaperBoundUserInterfaceState)state);
    }

    private void InputOnTextEntered(NetEntity editTool, string text)
    {
        var playerUid = PlayerManager.LocalEntity ?? EntityUid.Invalid;
        SendMessage(new PaperInputTextMessage(EntMan.GetNetEntity(playerUid), editTool, text));
        _window?.Clear();
    }

    private void InputOnFullTextEntered(string text)
    {
        SendMessage(new PaperInputFullTextMessage(text));
        _window?.Clear();
    }
}
