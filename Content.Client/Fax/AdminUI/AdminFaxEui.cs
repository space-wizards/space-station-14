using Content.Client.Eui;
using Content.Shared.Eui;
using Content.Shared.Fax;
using JetBrains.Annotations;

namespace Content.Client.Fax.AdminUI;

[UsedImplicitly]
public sealed class AdminFaxEui : BaseEui
{
    private readonly AdminFaxWindow _window;

    public AdminFaxEui()
    {
        _window = new AdminFaxWindow();
        _window.OnClose += () => SendMessage(new AdminFaxEuiMsg.Close());
    }

    public override void Opened()
    {
        _window.OpenCentered();
    }

    public override void Closed()
    {
        _window.Close();
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not AdminFaxEuiState cast)
            return;
        _window.Populate(cast.Entries);
    }
}
