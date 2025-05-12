using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.SpawnedFromTracker;

[RegisterComponent, NetworkedComponent]
public sealed partial class SpawnedFromTrackerComponent : Component
{
    public EntityUid SpawnedFrom;
}
