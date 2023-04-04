using System.IO;
using Content.Shared.Chemistry.Reagent;
using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Chemistry;

/// <summary>
/// This handles the chemistry guidebook and caching it.
/// </summary>
public sealed class ChemistryGuideDataSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;

    private Dictionary<string, ReagentGuideEntry> _reagentGuideRegistry = new();

    public IReadOnlyDictionary<string, ReagentGuideEntry> ReagentGuideRegistry => _reagentGuideRegistry;

    /// <inheritdoc/>
    public override void Initialize()
    {
        if (_net.IsServer)
        {
            _net.RegisterNetMessage<MsgUpdateReagentGuideRegistry>(OnReceiveRegistryUpdate);
        }
    }

    private void OnReceiveRegistryUpdate(MsgUpdateReagentGuideRegistry message)
    {
        var data = message.Changeset;

    }
}

public sealed class MsgUpdateReagentGuideRegistry : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.String;
    // This could break with prototype loads if unordered.
    public override NetDeliveryMethod DeliveryMethod { get; } = NetDeliveryMethod.ReliableOrdered;

    public ReagentGuideChangeset? Changeset { get; set; }

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        var length = buffer.ReadVariableInt32();
        using var stream = buffer.ReadAlignedMemory(length);
        Changeset = serializer.Deserialize<ReagentGuideChangeset>(stream);
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        var stream = new MemoryStream();
        DebugTools.AssertNotNull(Changeset);
        serializer.Serialize(stream, Changeset!);

        buffer.WriteVariableInt32((int)stream.Length);
        buffer.Write(stream.AsSpan());
    }
}

[Serializable, NetSerializable]
public sealed class ReagentGuideChangeset
{
    public Dictionary<string,ReagentGuideEntry> GuideEntries;

    public HashSet<string> Removed;

    public ReagentGuideChangeset(Dictionary<string, ReagentGuideEntry> guideEntries, HashSet<string> removed)
    {
        GuideEntries = guideEntries;
        Removed = removed;
    }
}
