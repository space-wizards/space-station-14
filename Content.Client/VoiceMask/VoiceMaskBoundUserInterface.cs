using Content.Shared.VoiceMask;
using Robust.Client.GameObjects;

namespace Content.Client.VoiceMask;

public sealed class VoiceMaskBoundUserInterface : BoundUserInterface
{
    public VoiceMaskBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    private VoiceMaskNameChangeWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = new();

        _window.OpenCentered();
        _window.OnNameChange += OnNameSelected;
        _window.OnClose += Close;
    }

    private void OnNameSelected(string name)
    {
        SendMessage(new VoiceMaskChangeNameMessage(name));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not VoiceMaskBuiState cast || _window == null)
        {
            return;
        }

        _window.UpdateState(cast.Name);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        _window?.Close();
    }
}
