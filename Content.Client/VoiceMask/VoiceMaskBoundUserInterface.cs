using Content.Shared.VoiceMask;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client.VoiceMask;

public sealed class VoiceMaskBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    [ViewVariables]
    private VoiceMaskNameChangeWindow? _window;

    public VoiceMaskBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = new(_proto);

        _window.OpenCentered();
        _window.OnNameChange += OnNameSelected;
        _window.OnVerbChange += verb => SendMessage(new VoiceMaskChangeVerbMessage(verb));
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

        _window.UpdateState(cast.Name, cast.Verb);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        _window?.Close();
    }
}
