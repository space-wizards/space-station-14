using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class NotekeeperUiState : BoundUserInterfaceState
{
    public List<String> Notes;

    public NotekeeperUiState(List<string> notes)
    {
        Notes = notes;
    }
}
