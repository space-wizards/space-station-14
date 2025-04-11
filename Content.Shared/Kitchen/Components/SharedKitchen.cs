using Robust.Shared.Serialization;

namespace Content.Shared.Kitchen.Components;

[Serializable, NetSerializable]
public enum CookingDeviceType
{
    Microwave,
    Oven,
    Stove
}