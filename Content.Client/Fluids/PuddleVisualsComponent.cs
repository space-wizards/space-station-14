namespace Content.Client.Fluids
{
    [RegisterComponent]
    public sealed class PuddleVisualsComponent : Component
    {
        // Whether the underlying solution color should be used. True in most cases.
        [DataField("recolor")] public bool Recolor = true;
        // Whether this puddle is capable of using wet floor sparkles.
        [DataField("sparkly")] public bool Sparkly = false;

    }
}
