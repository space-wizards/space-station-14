using Content.Client.Administration.UI.AdminAnnounce;
using Content.Client.Eui;
using Content.Shared.Eui;

namespace Content.Client.Administration.UI;

public sealed class AdminAnnounceEui : BaseEui
{
    private readonly AdminAnnounceWindow _window;

    public AdminAnnounceEui()
    {
        _window = new AdminAnnounceWindow();
        _window.OnClose += () => SendMessage(new CloseEuiMessage());
        _window.OnAnnounce += SendMessage;
    }

    public override void Opened() => _window.OpenCentered();
    public override void Closed() => _window.Close();
}
