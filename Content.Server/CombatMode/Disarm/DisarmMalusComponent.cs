namespace Content.Server.CombatMode.Disarm
{
    [RegisterComponent]
    /// <summary>
    /// Applies a malus to disarm attempts against this item.
    /// </summary>
    public sealed class DisarmMalusComponent : Component
    {
        [DataField("malus")]
        public float Malus = 0.3f;
    }
}
