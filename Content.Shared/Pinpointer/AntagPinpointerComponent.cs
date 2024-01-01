using Robust.Shared.GameStates;

namespace Content.Shared.Pinpointer;

/// <summary>
/// Displays a sprite on the item that points towards the StealTarget component with the right StealGroup.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedPinpointerSystem))]
public sealed partial class AntagPinpointerComponent : Component
{

}
