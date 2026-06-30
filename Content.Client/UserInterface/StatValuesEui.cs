using Content.Client.Eui;
using Content.Shared.Eui;
using Content.Shared.UserInterface;

namespace Content.Client.UserInterface;

public sealed class StatValuesEui : BaseEui
{
    private readonly StatsWindow _window;

    public StatValuesEui()
    {
        _window = new StatsWindow();
        _window.Title = Loc.GetString("stat-values-ui-title");
        _window.OpenCentered();
        _window.OnClose += Closed;
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is not StatValuesEuiMessage eui)
            return;

        _window.Title = eui.Title;
        _window.UpdateValues(eui.Headers, eui.Values);
    }
}
