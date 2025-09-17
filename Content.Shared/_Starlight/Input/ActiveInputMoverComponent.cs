using Robust.Shared.GameStates;

namespace Content.Shared._Starlight.Input;

// Taken from https://github.com/RMC-14/RMC-14
[RegisterComponent, NetworkedComponent]
[Access(typeof(StarlightInputSystem))]
public sealed partial class ActiveInputMoverComponent : Component;
