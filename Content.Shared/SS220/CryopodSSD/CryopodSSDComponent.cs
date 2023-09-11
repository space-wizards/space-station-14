// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.SS220.CryopodSSD;


/// <summary>
/// Component for In-game leaving or AFK
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CryopodSSDComponent : Component
{
    /// <summary>
    /// Delay before climbing in cryopod
    /// </summary>
    [DataField("entryDelay")] public float EntryDelay = 6f;

    [ViewVariables(VVAccess.ReadWrite)] public TimeSpan EntityLiedInCryopodTime;

    [ViewVariables(VVAccess.ReadWrite)] public ContainerSlot BodyContainer = default!;

    [DataField("leaveAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>), required: true)]
    public string? LeaveAction;

    [DataField("leaveActionEntity")]
    public EntityUid? LeaveActionEntity;

    [Serializable, NetSerializable]
    public enum CryopodSSDVisuals : byte
    {
        ContainsEntity
    }
}

/// <summary>
/// Raises when somebody transfers to cryo storage
/// </summary>
public sealed class TransferredToCryoStorageEvent : HandledEntityEventArgs
{
    public EntityUid CryopodSSD { get; }
    public EntityUid EntityToTransfer { get; }

    public TransferredToCryoStorageEvent(EntityUid cryopodSsd, EntityUid entityToTransfer)
    {
        CryopodSSD = cryopodSsd;
        EntityToTransfer = entityToTransfer;
    }
}
