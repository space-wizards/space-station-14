using Content.Server.AI.Steering;
using Content.Server.AI.Systems;
using Robust.Shared.Map;

namespace Content.Server.AI.Components;

/// <summary>
/// Added to any NPCs that are steering (i.e. moving).
/// </summary>
[RegisterComponent]
public sealed class NPCSteeringComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)] public SteeringStatus Status = SteeringStatus.Pending;

    [ViewVariables(VVAccess.ReadWrite)] public EntityCoordinates Target;

    public GridTargetSteeringRequest Request = default!;
}

