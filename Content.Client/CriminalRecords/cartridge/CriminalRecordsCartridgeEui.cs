using Content.Client.Eui;
using Content.Shared.CriminalRecords;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Client.CriminalRecords;

[UsedImplicitly]
public sealed class CriminalRecordsCartridgeEui : BaseEui
{
    private readonly CriminalRecordsCartridgeUi _window;

    public CriminalRecordsCartridgeEui()
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

        if (state is not CriminalRecordsCartridgeUiState cast)
        {
            return;
        }

        _window.Populate(cast.Records);
    }
}
