using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Nutrition.Components;

/// <summary>
/// A component which is basically just a collection of <see cref="Satiation"/>s keyed by their
/// <see cref="SatiationTypePrototype"/>s.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
// Nothing should modify the dictionary once it's deserialized. Perhaps satiations can be dynamically
// added and removed in the future, but not today.
[Access(typeof(SatiationSystem))]
public sealed partial class SatiationComponent : Component
{
    [DataField(required: true)]
    [AutoNetworkedField]
    public Dictionary<ProtoId<SatiationTypePrototype>, Satiation> Satiations = [];

    /// <summary>
    /// Checks if this has a <see cref="Satiation"/> of the specified <paramref name="type"/>.
    /// </summary>
    [Access(Other = AccessPermissions.Execute)]
    public bool Has(ProtoId<SatiationTypePrototype> type) => GetOrNull(type) != null;

    /// <summary>
    /// Gets the <see cref="Satiation"/> of the given <paramref name="type"/> on this component, or
    /// <c>null</c> if no such satiation exists.
    /// </summary>
    [Access(Other = AccessPermissions.Execute)]
    public Satiation? GetOrNull(ProtoId<SatiationTypePrototype> type) => Satiations.GetValueOrDefault(type);
}
