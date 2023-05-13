using Robust.Shared.Serialization;

namespace Content.Shared.Lathe;

[Serializable, NetSerializable]
public sealed class LatheEjectMaterialMessage : BoundUserInterfaceMessage
{
    public string Material;

    public int Amount;

    public LatheEjectMaterialMessage(string material, int amount)
    {
        Material = material;
        Amount = amount;
    }
}
