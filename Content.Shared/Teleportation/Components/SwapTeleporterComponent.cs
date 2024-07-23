using Content.Shared.Teleportation.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Teleportation.Components;

/// <summary>
/// This is used for an entity that, when linked to another valid entity, allows the two to swap positions,
/// additionally swapping the positions of the parents.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SwapTeleporterSystem))]
public sealed partial class SwapTeleporterComponent : Component
{
    /// <summary>
    /// The other SwapTeleporterComponent that this one is linked to
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? LinkedEnt;

    /// <summary>
    /// the time at which <see cref="TeleportDelay"/> ends and the teleportation occurs
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public TimeSpan? TeleportTime;

    /// <summary>
    /// Delay after starting the teleport and it occuring.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan TeleportDelay = TimeSpan.FromSeconds(2.5f);

    /// <summary>
    /// The time at which <see cref="Cooldown"/> ends and teleportation can occur again.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    [AutoPausedField]
    public TimeSpan NextTeleportUse;

    /// <summary>
    /// A minimum waiting period inbetween teleports.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan Cooldown = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Sound played when teleportation begins
    /// </summary>
    [DataField]
    public SoundSpecifier? TeleportSound = new SoundPathSpecifier("/Audio/Weapons/flash.ogg");

    /// <summary>
    /// A whitelist for what entities are valid for <see cref="LinkedEnt"/>.
    /// </summary>
    [DataField]
    public EntityWhitelist TeleporterWhitelist = new();
}

[Serializable, NetSerializable]
public enum SwapTeleporterVisuals : byte
{
    Linked
}
