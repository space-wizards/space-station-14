using Content.Client.Eui;
using Content.Shared.CrewManifest;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Client.CrewManifest;

[UsedImplicitly]
public sealed class CrewManifestEui : BaseEui
{
    private readonly CrewManifestUi _window;

    public CrewManifestEui()
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

        if (state is not CrewManifestEuiState cast)
        {
            return;
        }

        _window.Populate(cast.StationName, cast.Entries);
    }
}
