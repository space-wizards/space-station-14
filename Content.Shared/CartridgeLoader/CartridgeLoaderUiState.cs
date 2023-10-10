using System.Collections.Immutable;
using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader;

[Virtual]
[Serializable, NetSerializable]
public class CartridgeLoaderUiState : BoundUserInterfaceState
{
    public NetEntity? ActiveUI;
    public List<NetEntity> Programs;

    public CartridgeLoaderUiState(List<NetEntity> programs, NetEntity? activeUI)
    {
        Programs = programs;
        ActiveUI = activeUI;
    }
}
