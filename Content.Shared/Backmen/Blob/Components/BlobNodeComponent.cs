using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Backmen.Blob.Components;
/// <remarks>
/// To add a new special blob tile you will need to change code in BlobNodeSystem and BlobTypedStorage.
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BlobNodeComponent : Component
{
    [DataField]
    public float PulseFrequency = 4f;

    [DataField]
    public float PulseRadius = 4f;

    public float NextPulse = 0;

    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public EntityUid? BlobResource = null;

    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public EntityUid? BlobFactory = null;
}

public sealed class BlobTileGetPulseEvent : HandledEntityEventArgs
{

}

[Serializable, NetSerializable]
public sealed partial class BlobMobGetPulseEvent : EntityEventArgs
{
    public NetEntity BlobEntity { get; set; }
}

/// <summary>
/// Event raised on all special tiles of Blob Node on pulse.
/// </summary>
public sealed class BlobSpecialGetPulseEvent : EntityEventArgs;

/// <summary>
/// Event
/// </summary>
public sealed class BlobNodePulseEvent : EntityEventArgs;
