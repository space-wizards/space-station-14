using Content.Client.Eui;
using Content.Shared.Eui;
using Content.Shared.Procedural;
using JetBrains.Annotations;

namespace Content.Client.Procedural;

[UsedImplicitly]
public class OverworldDebugEui : BaseEui
{
    private OverworldDebugWindow _window;
    public OverworldDebugEui()
    {
        _window = new OverworldDebugWindow();
        _window.ZoomSlider.OnValueChanged += _ =>
        {
            SendSettings();
        };
    }

    private void SendSettings()
    {
        SendMessage(new OverworldDebugSettingsMessage()
        {
            Zoom = _window.ZoomSlider.Value,
        });
    }

    public override void Opened()
    {
        base.Opened();
        _window.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();
        _window.Close();
    }

    public override void HandleState(EuiStateBase state)
    {
        base.HandleState(state);

        if (state is not OverworldDebugEuiState worldState) return;

        _window.UpdateState(worldState);
    }
}
