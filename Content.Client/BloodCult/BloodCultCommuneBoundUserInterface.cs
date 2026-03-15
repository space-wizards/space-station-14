using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;
using Content.Shared.BloodCult;

namespace Content.Client.BloodCult;

/// <summary>
/// Handles the commune UI for blood cultists.
/// This commune UI is used to cause cult communications to mask as chanting to nearby people
/// It's also used to control how loud that chanting is, so later stages of the cult speak audibly when using cult speech
/// </summary>
public sealed class BloodCultCommuneBoundUserInterface : BoundUserInterface
{
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
}
