using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;
using Content.Shared.BloodCult;

namespace Content.Client.BloodCult;

public sealed class BloodCultCommuneBoundUserInterface : BoundUserInterface
{
    //[Dependency] private readonly IPrototypeManager _protomanager = default!;

    [ViewVariables]
    private BloodCultCommuneWindow? _window;

    public BloodCultCommuneBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<BloodCultCommuneWindow>();
        _window.OnCommune += OnCommuneSent;
    }

    private void OnCommuneSent(string message)
    {
		if (message.Length > 0)
		{
			SendMessage(new BloodCultCommuneSendMessage(message));
			_window?.Close();
		}
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not BloodCultCommuneBuiState cast || _window == null)
        {
            return;
        }

        _window.UpdateState(cast.Message);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        _window?.Close();
    }
}
