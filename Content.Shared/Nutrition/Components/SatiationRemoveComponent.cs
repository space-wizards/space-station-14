using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Nutrition.Components;

/// <summary>
/// Removes satiation types from an entity on map init
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SatiationModifySystem))]
public sealed partial class SatiationRemoveComponent : Component
{
    /// <summary>
    /// The list of satiation types to remove from this entity on <see cref="MapInitEvent"/>.
    /// </summary>
    [DataField(required: true), AutoNetworkedField, AlwaysPushInheritance]
    public Dictionary<ProtoId<SatiationTypePrototype>, Satiation> Satiation = new();
}
