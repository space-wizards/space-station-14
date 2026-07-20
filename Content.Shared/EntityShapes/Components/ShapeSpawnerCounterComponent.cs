using Robust.Shared.GameStates;

namespace Content.Shared.EntityShapes.Components;

/// <summary>
/// Used for different shape spawner components to count new steps for spawns.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ShapeSpawnerCounterComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan SpawnPeriod = TimeSpan.FromSeconds(1f);

    [DataField, AutoNetworkedField]
    public int MaxCounter = 1;

    [ViewVariables, AutoNetworkedField]
    public TimeSpan NextSpawn;

    [ViewVariables, AutoNetworkedField]
    public int Counter = 1; // We spawn 1 shape not in a loop, so we have to start from 1.
}
