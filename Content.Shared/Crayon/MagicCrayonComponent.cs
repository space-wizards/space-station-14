using Robust.Shared.Prototypes;

namespace Content.Shared.Crayon;

[RegisterComponent]
public sealed partial class MagicCrayonComponent : Component
{
    [DataField]
    public EntProtoId SpawnProto = "FoodBurgerCheese";
}
