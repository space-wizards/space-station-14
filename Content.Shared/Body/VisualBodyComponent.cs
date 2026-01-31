using Robust.Shared.GameStates;

namespace Content.Shared.Body;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedVisualBodySystem))]
public sealed partial class VisualBodyComponent : Component;
