using Robust.Shared.Serialization;

namespace Content.Shared.Laundry;

[RegisterComponent]
public partial class SharedWashingMachineComponent : Component { } //Hi, I'm no coder but the word "partial" used to be "sealed" o3o

[Serializable, NetSerializable]
public enum WashingMachineVisualState : byte
{
    Broken,
}