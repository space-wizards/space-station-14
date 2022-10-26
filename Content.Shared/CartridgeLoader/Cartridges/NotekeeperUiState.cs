using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class NotekeeperUiState : BoundUserInterfaceState
{
    public List<String> Messages;

    public NotekeeperUiState(List<string> messages)
    {
        Messages = messages;
    }
}
