using System.Numerics;
using Content.Shared.Roles;
using Robust.Shared.Audio;
using Robust.Shared.Containers;

namespace Content.Server.CryoSleep;
[RegisterComponent]
public sealed partial class CryoSleepComponent : Component
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
    /// How long the entity initially is asleep for upon joining.
    /// </summary>
    [DataField("initialSleepDurationRange")]
    public Vector2 InitialSleepDurationRange = new (5, 10);
}
