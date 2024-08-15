using Content.Client.Eui;
using Content.Shared.Eui;
using Content.Shared.ReadyManifest;
using JetBrains.Annotations;

namespace Content.Client.ReadyManifest;

[UsedImplicitly]
public sealed class ReadyManifestEui : BaseEui
{
    private readonly ReadyManifestUi _window;

    public ReadyManifestEui()
    {
        _window = new();

        _window.OnClose += () =>
        {
            SendMessage(new CloseEuiMessage());
        };
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

        if (state is not ReadyManifestEuiState cast)
        {
            return;
        }
        _window.RebuildUI(cast.JobCounts);
    }
}