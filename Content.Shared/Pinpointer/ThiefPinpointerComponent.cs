using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Pinpointer;

/// <summary>
/// Displays a sprite on the item that points towards the StealTarget component with the right StealGroup.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedPinpointerSystem))]
public sealed partial class ThiefPinpointerComponent : Component
{
}
