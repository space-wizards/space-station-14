using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class MessagesUiState : BoundUserInterfaceState
{
    public List<string> Notes;

    public MessagesUiState(List<string> notes)
    {
        Notes = notes;
    }
}
