
namespace Content.Client.Fluids
{
    [RegisterComponent]
    public sealed class PuddleVisualsComponent : Component
    {

        // Whether the underlying solution color should be used. True in most cases.
        [DataField("recolor")] public bool Recolor = true;

    }

}
