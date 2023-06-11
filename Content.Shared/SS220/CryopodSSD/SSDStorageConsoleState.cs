// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Serialization;
using static Content.Shared.Storage.SharedStorageComponent;
namespace Content.Shared.SS220.CryopodSSD;

[Serializable, NetSerializable]
public enum SSDStorageConsoleKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class SSDStorageConsoleState : BoundUserInterfaceState
{
    public bool HasAccess { get; }
    public StorageBoundUserInterfaceState StorageState { get; }
    public List<string> CryopodSSDRecords { get; }

    public SSDStorageConsoleState(bool hasAccess, List<string> cryopodSSDRecords, StorageBoundUserInterfaceState storageState)
    {
        HasAccess = hasAccess;
        CryopodSSDRecords = cryopodSSDRecords;
        StorageState = storageState;
    }
}