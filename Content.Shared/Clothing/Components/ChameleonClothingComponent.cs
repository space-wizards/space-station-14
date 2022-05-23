using Robust.Shared.Serialization;

namespace Content.Shared.Clothing.Components;

[RegisterComponent]
public sealed class ChameleonClothingComponent : Component
{

}

[Serializable, NetSerializable]
public enum ChameleonVisuals : byte
{
    ClothingId
}

[Serializable, NetSerializable]
public enum ChameleonUiKey : byte
{
    Key
}
