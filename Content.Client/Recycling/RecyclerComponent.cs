using Content.Shared.Recycling;

namespace Content.Client.Recycling
{
    // TODO: Add sound and safe beep
    [RegisterComponent]
    [ComponentReference(typeof(SharedRecyclerComponent))]
    public sealed class RecyclerComponent : SharedRecyclerComponent
    {
        // The sprite layer to use when the recycler is on
        [DataField("state_on")]
        public readonly string SpriteLayerOn = "grinder-o1";

        // The sprite layer to use when the recycler is off
        [DataField("state_off")]
        public readonly string SpriteLayerOff = "grinder-o0";
    }
}

