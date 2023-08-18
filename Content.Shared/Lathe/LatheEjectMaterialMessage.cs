using Robust.Shared.Serialization;

namespace Content.Shared.Lathe;

[Serializable, NetSerializable]
public sealed class LatheEjectMaterialMessage : BoundUserInterfaceMessage
{
    public string Material;
    public int SheetsToExtract;

    public LatheEjectMaterialMessage(string material, int sheetsToExtract)
    {
        Material = material;
        SheetsToExtract = sheetsToExtract;
    }
}
