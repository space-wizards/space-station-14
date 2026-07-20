using Robust.Shared.GameStates;

namespace Content.Shared.EntityShapes.Components;

/// <summary>
/// Used for different shape spawner components to count new steps for spawns.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ShapeSpawnerCounterComponent : Component
{
    /// <summary>
    /// Prediod between each trigger.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan SpawnPeriod = TimeSpan.FromSeconds(1f);

    /// <summary>
    /// The max amount of triggers.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MaxCounter = 2;

    /// <summary>
    /// Time when the next trigger will occur.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public TimeSpan NextSpawn;

    /// <summary>
    /// Amount of triggers.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public int Counter = 1; // We spawn 1 shape on map init already
}
