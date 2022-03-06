namespace Content.Client.Fluids
{
    [RegisterComponent]
    public sealed class PuddleVisualsComponent : Component
    {
        // Whether the underlying solution color should be used. True in most cases.
        [DataField("recolor")] public bool Recolor = true;

        // Whether this puddle is capable of using wet floor sparkles.
        [DataField("sparkly")] public bool Sparkly = false;

        [DataField("effectRsi")] public string EffectRsi = "Fluids/wet_floor_sparkles.rsi";

        [DataField("effectState")] public string EffectState = "sparkles";
    }
}
