using Content.Shared.Clothing.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Clothing.Components;

/// <summary>
///     This component indicates that this clothing is attached to some other entity with a <see
///     cref="ToggleableClothingComponent"/>. When unequipped, this entity should be returned to the entity that it is
///     attached to, rather than being dumped on the floor or something like that. Intended for use with hardsuits and
///     hardsuit helmets.
/// </summary>
[Access(typeof(ToggleableClothingSystem))]
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AttachedClothingComponent : Component
{
    /// <summary>
    ///     The Id of the piece of clothing that this entity belongs to.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid AttachedUid;
}
