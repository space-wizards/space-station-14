using Content.Shared.Recycling;

namespace Content.Client.Recycling
{
    // TODO: Add sound and safe beep
    [RegisterComponent]
    [ComponentReference(typeof(SharedRecyclerComponent))]
    public sealed class RecyclerComponent : SharedRecyclerComponent
    {
    }

    public enum RecyclerVisualLayers : byte
    {
        ConveyorOff,
        ConveyorOn,
    }
}

