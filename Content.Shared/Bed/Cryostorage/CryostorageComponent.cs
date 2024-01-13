using Robust.Shared.Serialization;

namespace Content.Shared.Bed.Cryostorage;

/// <summary>
/// This is used for a container which, when a player logs out while inside of,
/// will delete their body and redistribute their items.
/// </summary>
[RegisterComponent]
public sealed partial class CryostorageComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string ContainerId = "storage";

    /// <summary>
    /// How long a player can remain inside Cryostorage before automatically being taken care of.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan GracePeriod = TimeSpan.FromMinutes(1f); //todo testing only. change to 5.
}

[Serializable, NetSerializable]
public enum CryostorageVisuals : byte
{
    Full
}

