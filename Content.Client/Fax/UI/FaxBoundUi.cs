using System.IO;
using System.Threading.Tasks;
using Content.Shared.Fax;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client.Fax.UI;

[UsedImplicitly]
public sealed class FaxBoundUi : BoundUserInterface
{
    [Dependency] private readonly IFileDialogManager _fileDialogManager = default!;

    [ViewVariables]
    private FaxWindow? _window;

    private bool _dialogIsOpen = false;

    public FaxBoundUi(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<FaxWindow>();
        _window.FileButtonPressed += OnFileButtonPressed;
        _window.CopyButtonPressed += OnCopyButtonPressed;
        _window.SendButtonPressed += OnSendButtonPressed;
        _window.RefreshButtonPressed += OnRefreshButtonPressed;
        _window.PeerSelected += OnPeerSelected;
    }

    private async void OnFileButtonPressed()
    {
        if (_dialogIsOpen)
            return;

        _dialogIsOpen = true;
        var filters = new FileDialogFilters(new FileDialogFilters.Group("txt"));
        await using var file = await _fileDialogManager.OpenFile(filters);
        _dialogIsOpen = false;

        if (_window == null || _window.Disposed || file == null)
        {
            return;
        }

        using var reader = new StreamReader(file);

        var firstLine = await reader.ReadLineAsync();
        string? label = null;
        var content = await reader.ReadToEndAsync();

        if (firstLine is { })
        {
            if (firstLine.StartsWith('#'))
            {
                label = firstLine[1..].Trim();
            }
            else
            {
                content = firstLine + "\n" + content;
            }
        }

        SendMessage(new FaxFileMessage(
            label?[..Math.Min(label.Length, FaxFileMessageValidation.MaxLabelSize)],
            content[..Math.Min(content.Length, FaxFileMessageValidation.MaxContentSize)],
            _window.OfficePaper));
    }

    private void OnSendButtonPressed()
    {
        SendMessage(new FaxSendMessage());
    }

    private void OnCopyButtonPressed()
    {
        SendMessage(new FaxCopyMessage());
    }

    private void OnRefreshButtonPressed()
    {
        SendMessage(new FaxRefreshMessage());
    }

    private void OnPeerSelected(string address)
    {
        SendMessage(new FaxDestinationMessage(address));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_window == null || state is not FaxUiState cast)
            return;

        _window.UpdateState(cast);
    }
}
