namespace Content.Shared.Item
{
    /// <summary>
    ///     Given to mobs that should be able to picked up after death. SHOULDN'T BE GIVEN TO ALREADY ITEM-MOBS
    /// </summary>
    [RegisterComponent]
    public sealed partial class MakeItemOnDeadComponent : Component
    {
    }
}

