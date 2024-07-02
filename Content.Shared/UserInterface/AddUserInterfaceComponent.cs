using Robust.Shared.GameStates;

namespace Content.Shared.UserInterface;

/// <summary>
/// On mapinit adds userinterfaces to the entity and then removes this component from the entity.
/// </summary>
/// <remarks>
/// Engine issue #5141 being implemented would make this obsolete
/// </remarks>
[RegisterComponent, NetworkedComponent, Access(typeof(AddUserInterfaceSystem))]
public sealed partial class AddUserInterfaceComponent : Component
{
    /// <summary>
    /// Each UI to add.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<Enum, InterfaceData> Interfaces = new();
}
