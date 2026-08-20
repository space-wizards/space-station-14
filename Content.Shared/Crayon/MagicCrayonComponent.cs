using Robust.Shared.Prototypes;

namespace Content.Shared.Crayon;

[RegisterComponent]
public sealed partial class MagicCrayonComponent : Component
{
    /// <summary>
    /// The entity prototype that will be spawned by this magic crayon.
    /// </summary>
    [DataField]
    public EntProtoId SpawnProto = "FoodBurgerCheese";
}
