using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed partial class NotekeeperUiState : BoundUserInterfaceState
{
    public List<string> Notes;

    public NotekeeperUiState(List<string> notes)
    {
        Notes = notes;
    }
}

