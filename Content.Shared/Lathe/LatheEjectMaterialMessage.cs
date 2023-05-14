using Robust.Shared.Serialization;

namespace Content.Shared.Lathe;

[Serializable, NetSerializable]
public sealed class LatheEjectMaterialMessage : BoundUserInterfaceMessage
{
    public string Material;

    public int WholeVolume;

    public int? ExtractedAmount;

    public LatheEjectMaterialMessage(string material, int volume, int? extractedAmount = null)
    {
        Material = material;
        WholeVolume = volume;
        ExtractedAmount = extractedAmount;
    }
}
