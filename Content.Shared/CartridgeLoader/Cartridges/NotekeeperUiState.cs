using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class NotekeeperUiState : IBoundUserInterfaceState
{
    public List<string> Notes;

    public NotekeeperUiState(List<string> notes)
    {
        Notes = notes;
    }
}
