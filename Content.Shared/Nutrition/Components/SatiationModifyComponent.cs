using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Nutrition.Components;

/// <summary>
/// Grants an entity satiation types on map init
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SatiationModifySystem))]
public sealed partial class SatiationModifyComponent : Component
{
    /// <summary>
    /// The list of satiation types to add to this entity on <see cref="MapInitEvent"/>.
    /// </summary>
    [DataField, AutoNetworkedField, AlwaysPushInheritance]
    public Dictionary<ProtoId<SatiationTypePrototype>, Satiation>? SatiationGrant = new();

    /// <summary>
    /// The list of satiation types to add to this entity on <see cref="MapInitEvent"/>.
    /// </summary>
    [DataField, AutoNetworkedField, AlwaysPushInheritance]
    public Dictionary<ProtoId<SatiationTypePrototype>, Satiation>? SatiationRemove = new();

    /// <summary>
    /// Whether the satiation(s) in SatiationGrant should be removed on <see cref="ComponentShutdown"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool RemoveOnShutdown = true;
}
