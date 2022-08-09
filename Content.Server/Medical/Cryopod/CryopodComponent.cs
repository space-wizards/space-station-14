using Robust.Shared.Audio;
using Robust.Shared.Containers;

namespace Content.Server.Medical.Cryopod;
[RegisterComponent]
public sealed class CryopodComponent : Component
{
    public ContainerSlot BodyContainer = default!;

    /// <summary>
    /// Whether or not spawns are routed through the cryopod.
    /// </summary>
    [DataField("doSpawns")]
    public bool DoSpawns = true;

    /// <summary>
    /// The sound that is played when a player spawns in the pod.
    /// </summary>
    [DataField("arrivalSound")]
    public SoundSpecifier ArrivalSound = new SoundPathSpecifier("/Audio/Effects/teleport_arrival.ogg");

    /// <summary>
    /// The sound that is played when a player leaves the game via a pod.
    /// </summary>
    [DataField("leaveSound")]
    public SoundSpecifier LeaveSound = new SoundPathSpecifier("/Audio/Effects/radpulse1.ogg");

    /// <summary>
    /// How long the entity initially is asleep for upon joining.
    /// </summary>
    [DataField("initialSleepDurationRange")]
    public Vector2 InitialSleepDurationRange = (5, 10);
}
