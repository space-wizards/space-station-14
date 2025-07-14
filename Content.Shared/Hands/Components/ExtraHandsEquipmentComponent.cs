using Robust.Shared.GameStates;
using Content.Shared.Hands.EntitySystems;

namespace Content.Shared.Hands.Components;

/// <summary>
/// An entity with this component will give you extra hands when you equip it in your inventory.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ExtraHandsEquipmentSystem))]
public sealed partial class ExtraHandsEquipmentComponent : Component
{
    /// <summary>
    /// Dictionary relating a unique hand ID corresponding to a container slot on the attached entity to a struct containing information about the Hand itself.
    /// </summary>
    [DataField]
    public Dictionary<string, Hand> Hands = new();
}
