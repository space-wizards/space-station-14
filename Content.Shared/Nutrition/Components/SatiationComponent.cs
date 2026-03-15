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
// Nothing can modify the dictionary once it's deserialized. Perhaps satiations can be dynamically
// added and removed in the future, but not today.
[Access]
public sealed partial class SatiationComponent : Component
{
    [DataField(required: true)]
    [AutoNetworkedField]
    public Dictionary<ProtoId<SatiationTypePrototype>, Satiation> Satiations = [];

    /// <summary>
    /// Checks if this has a <see cref="Satiation"/> of the specified <paramref name="type"/>.
    /// </summary>
    [Access(Other = AccessPermissions.ReadExecute)]
    public bool Has(ProtoId<SatiationTypePrototype> type) => GetOrNull(type) != null;

    [Access(Other = AccessPermissions.ReadExecute)]
    public Satiation? GetOrNull(ProtoId<SatiationTypePrototype> type) => Satiations.GetValueOrDefault(type);
}
