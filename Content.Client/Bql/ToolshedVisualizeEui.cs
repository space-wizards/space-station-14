using Content.Client.Eui;
using Content.Shared.Bql;
using Content.Shared.Eui;
using JetBrains.Annotations;
using Robust.Client.Console;

namespace Content.Client.Bql;

[UsedImplicitly]
public sealed class ToolshedVisualizeEui : BaseEui
{
    private readonly ToolshedVisualizeWindow _window;

    public ToolshedVisualizeEui()
    {
        _window = new ToolshedVisualizeWindow(
            IoCManager.Resolve<IClientConsoleHost>(),
            IoCManager.Resolve<ILocalizationManager>()
        );

        _window.OnClose += () => SendMessage(new CloseEuiMessage());
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not ToolshedVisualizeEuiState castState)
            return;

        _window.Update(castState.Entities);
    }

    public override void Closed()
    {
        base.Closed();

        _window.Close();
    }

    public override void Opened()
    {
        base.Opened();

        _window.OpenCentered();
    }
}
