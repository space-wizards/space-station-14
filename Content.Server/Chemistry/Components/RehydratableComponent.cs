namespace Content.Server.Chemistry.Components
{
    /// <summary>
    /// Basically, monkey cubes.
    /// But specifically, this component deletes the entity and spawns in a new entity when the entity is exposed to a given reagent.
    /// </summary>
    [RegisterComponent]
    public sealed class RehydratableComponent : Component
    {
        [DataField("catalyst")]
        internal string CatalystPrototype = "Water";

        [DataField("target")]
        internal string? TargetPrototype = default!;

        internal bool Expanding;
    }
}
