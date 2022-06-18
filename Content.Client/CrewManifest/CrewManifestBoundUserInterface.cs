using Content.Shared.CrewManifest;
using Robust.Client.GameObjects;

namespace Content.Client.CrewManifest;

public sealed class CrewManifestBoundUserInterface : BoundUserInterface
{
    private CrewManifestUi? _ui;

    public CrewManifestBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
    {}

    protected override void Open()
    {
        base.Open();

        _ui = new();
        _ui.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not CrewManifestBoundUiState cast)
        {
            return;
        }

        _ui!.Populate(cast.Entries);
    }
}
