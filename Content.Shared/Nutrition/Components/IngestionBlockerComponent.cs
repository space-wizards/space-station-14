using Content.Shared.Nutrition.EntitySystems;

namespace Content.Shared.Nutrition.Components;

/// <summary>
///     Component that denotes a piece of clothing that blocks the mouth or otherwise prevents eating & drinking.
/// </summary>
/// <remarks>
///     In the event that more head-wear & mask functionality is added (like identity systems, or raising/lowering of
///     masks), then this component might become redundant.
/// </remarks>
[RegisterComponent, Access(typeof(IngestionSystem))]
public sealed partial class IngestionBlockerComponent : Component
{
    /// <summary>
    ///     Is this component currently blocking consumption.
    /// </summary>
    [DataField]
    public bool Enabled { get; set; } = true;
}
