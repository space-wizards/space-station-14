using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Anomaly.Effects.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(BluespaceAnomalySystem))]
public sealed class BluespaceAnomalyComponent : Component
{
    /// <summary>
    /// The maximum radius that the shuffle effect will extend for
    /// scales with stability
    /// </summary>
    [DataField("maxShuffleRadius"), ViewVariables(VVAccess.ReadWrite)]
    public float MaxShuffleRadius = 10;

    /// <summary>
    /// The maximum MAX distance the portal this anomaly is tied to can teleport you.
    /// </summary>
    [DataField("maxPortalRadius"), ViewVariables(VVAccess.ReadWrite)]
    public float MaxPortalRadius = 25;

    /// <summary>
    /// The minimum MAX distance the portal this anomaly is tied to can teleport you.
    /// </summary>
    [DataField("minPortalRadius"), ViewVariables(VVAccess.ReadWrite)]
    public float MinPortalRadius = 10;

    /// <summary>
    /// How far the supercritical event can teleport you
    /// </summary>
    [DataField("superCriticalTeleportRadius"), ViewVariables(VVAccess.ReadWrite)]
    public float SupercriticalTeleportRadius = 50f;

    /// <summary>
    /// The sound played after players are shuffled/teleported around
    /// </summary>
    [DataField("teleportSound"), ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier TeleportSound = new SoundPathSpecifier("/Audio/Effects/teleport_arrival.ogg");
}
