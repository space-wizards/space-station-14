using Robust.Shared.GameObjects;
using Robust.Shared.Analyzers;
using Robust.Shared.ViewVariables;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Nutrition.EntitySystems;

/// <summary>
///     Component that denotes a piece of clothing that blocks the mouth or otherwise prevents eating & drinking.
/// </summary>
/// <remarks>
///     In the event that more head-wear & mask functionality is added (like identity systems, or raising/lowering of
///     masks), then this component might become redundant.
/// </remarks>
[RegisterComponent, Friend(typeof(FoodSystem), typeof(DrinkSystem))]
public sealed class IngestionBlockerComponent : Component
{
    /// <summary>
    ///     Is this component currently blocking consumption.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("enabled")]
    public bool Enabled { get; set; } = true;
}
