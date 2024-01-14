using Robust.Shared.Audio;
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
    /// How long a player can remain inside Cryostorage before automatically being taken care of, given that they have no mind.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NoMindGracePeriod = TimeSpan.FromSeconds(5f); //todo fix both these values

    /// <summary>
    /// How long a player can remain inside Cryostorage before automatically being taken care of.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan GracePeriod = TimeSpan.FromMinutes(0.1f);

    /// <summary>
    /// Sound that is played when a player is removed by a cryostorage.
    /// </summary>
    [DataField]
    public SoundSpecifier? RemoveSound = new SoundPathSpecifier("/Audio/Effects/teleport_departure.ogg");
}

[Serializable, NetSerializable]
public enum CryostorageVisuals : byte
{
    Full
}

