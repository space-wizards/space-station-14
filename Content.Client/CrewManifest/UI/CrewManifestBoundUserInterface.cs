using Content.Shared.CrewManifest;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.CrewManifest.UI;

[UsedImplicitly]
public sealed class CrewManifestBoundUserInterface : BoundUserInterface
{
    private EntityUid _owner;

    private CrewManifestUi? _menu;

    public CrewManifestBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _owner = owner;
    }

    protected override void Open()
    {
        base.Open();
        _menu = this.CreateWindow<CrewManifestUi>();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is not CrewManifestBuiState msg)
            return;

        _menu?.Populate(msg.StationName, msg.Entries);
        
    }
}