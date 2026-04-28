using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Nutrition.Components;

/// <summary>
///     Component that denotes a piece of clothing that blocks the mouth or otherwise prevents eating & drinking.
/// </summary>
/// <remarks>
///     In the event that more head-wear & mask functionality is added (like identity systems, or raising/lowering of
///     masks), then this component might become redundant.
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(IngestionSystem))]
public sealed partial class IngestionBlockerComponent : Component
{
    /// <summary>
    ///     Whether this item currently blocks consuming something.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled = true;
}
