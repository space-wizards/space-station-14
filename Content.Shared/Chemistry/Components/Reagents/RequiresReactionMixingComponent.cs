using Content.Shared.Chemistry.Reaction;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chemistry.Components.Reagents;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RequiresReactionMixingComponent : Component
{
    /// <summary>
    ///     The required mixing categories for an entity to mix the solution with for the reaction to occur
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public List<ProtoId<MixingCategoryPrototype>> MixingCategories = default!;
}
