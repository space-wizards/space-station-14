using Lidgren.Network;

namespace Content.Shared.OpaqueId;

public interface IOpaqueId
{
    public uint InternalId { get; set; }
}
