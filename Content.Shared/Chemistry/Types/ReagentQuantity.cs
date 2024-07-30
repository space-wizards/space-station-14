using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Types;


[DataRecord, Serializable, NetSerializable]
public partial struct ReagentQuantity
{
    [DataField]
    public ReagentDef ReagentId;
    [DataField]
    public FixedPoint2 Quantity;
    [DataField]
    public List<ReagentMetadata>? Metadata;
    [DataField]
    public List<FixedPoint2>? MetadataVolumes;

    public ReagentQuantity(
        ReagentDef id,
        FixedPoint2 quantity,
        (List<ReagentMetadata> metadata,List<FixedPoint2> metadataVolumes)? metadata = null)
    {
        ReagentId = id;
        Quantity = quantity;
        if (metadata == null)
            return;
        Metadata = metadata.Value.metadata;
        MetadataVolumes = metadata.Value.metadataVolumes;
    }

    public ReagentQuantity(
        ReagentDef id,
        FixedPoint2 quantity,
        List<(ReagentMetadata, FixedPoint2)>? metadata = null)
    {
        ReagentId = id;
        Quantity = quantity;
        if (metadata == null)
            return;
        Metadata = new();
        MetadataVolumes = new();
        foreach (var (data, volume) in metadata)
        {
            Metadata.Add(data);
            MetadataVolumes.Add(volume);
        }
    }
}
