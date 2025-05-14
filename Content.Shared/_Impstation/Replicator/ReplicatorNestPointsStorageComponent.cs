using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.Replicator;

[RegisterComponent, NetworkedComponent]
public sealed partial class ReplicatorNestPointsStorageComponent : Component
{
    public int TotalPoints;

    public int TotalReplicators;

    public int Level;
}
