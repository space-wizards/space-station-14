using Content.Client.Eui;

namespace Content.Client.NPC;

public sealed class NPCEui : BaseEui
{
    private NPCWindow? _window = new();

    public override void Opened()
    {
        base.Opened();
        _window = new NPCWindow();
        _window.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();
        _window?.Close();
        _window = null;
    }
}
