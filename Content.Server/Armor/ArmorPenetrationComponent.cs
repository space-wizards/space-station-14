namespace Content.Server.Armor
{
    [RegisterComponent]
    public sealed class ArmorPenetrationComponent : Component
    {
    [DataField("armorPenetrationValue")]
    public float? ArmorPenetrationValue;
    }
}
