using Content.Shared.Xenobiology.UI;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Xenobiology.UI;

[UsedImplicitly]
public sealed class CellSequencerBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private CellSequencerWindow? _window;

    public CellSequencerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<CellSequencerWindow>();

        _window.OnSync += () => SendMessage(new CellSequencerUiSyncMessage());

        _window.OnScan += () => SendMessage(new CellSequencerUiScanMessage());
        _window.OnCopy += () => SendMessage(new CellSequencerUiCopyMessage());

        _window.OnAdd += () => SendMessage(new CellSequencerUiAddMessage());
        _window.OnRemove += () => SendMessage(new CellSequencerUiRemoveMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not CellSequencerUiState sequencerUiState)
            return;

        _window?.UpdateState(sequencerUiState);
    }
}
