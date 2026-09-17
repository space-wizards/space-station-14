using System.IO;
using Content.Shared.Fax;
using Content.Shared.Fax.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Fax.UI;

[UsedImplicitly]
public sealed partial class FaxBoundUi : BoundUserInterface
{
    [Dependency] private IFileDialogManager _fileDialogManager = default!;
    [Dependency] private FaxSystem _fax = default!;

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
        await using var file = await _fileDialogManager.OpenFile(filters, FileAccess.Read);
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

        SendPredictedMessage(new FaxFileMessage(
            label?[..Math.Min(label.Length, FaxFileMessageValidation.MaxLabelSize)],
            content[..Math.Min(content.Length, FaxFileMessageValidation.MaxContentSize)],
            _window.OfficePaper));
    }

    private void OnSendButtonPressed()
    {
        SendPredictedMessage(new FaxSendMessage());
    }

    private void OnCopyButtonPressed()
    {
        SendPredictedMessage(new FaxCopyMessage());
    }

    private void OnRefreshButtonPressed()
    {
        SendPredictedMessage(new FaxRefreshMessage());
    }

    private void OnPeerSelected(string address)
    {
        SendPredictedMessage(new FaxDestinationMessage(address));
    }

    public override void Update()
    {
        base.Update();

        if (_window == null)
            return;

        if (!EntMan.TryGetComponent<FaxMachineComponent>(Owner, out var fax))
            return;

        _fax.TryGetInserted((Owner, fax), out var paper);
        var cooldown = _fax.PrintCooldown((Owner, fax));

        _window.Update(cooldown,
            cooldown || fax.DestinationAddress == null,
            fax.Name,
            EntMan.GetComponentOrNull<MetaDataComponent>(paper)?.EntityName,
            fax.KnownFaxes,
            fax.DestinationAddress);
    }
}
