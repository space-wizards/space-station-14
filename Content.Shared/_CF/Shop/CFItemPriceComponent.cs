namespace Content.Shared.CF.Shop;

/// <summary>Shop price in copper. 100 copper is 1 gp. Read from the entity prototype.</summary>
[RegisterComponent]
public sealed partial class CFItemPriceComponent : Component
{
    [DataField]
    public int Copper;
}
