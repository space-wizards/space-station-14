using Robust.Shared.GameStates;

namespace Content.Shared.Chasm.Components;

/// <summary>
/// When an entity falls into  chasm that has this component, it gets simply deleted.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ChasmDeleteComponent : Component;
