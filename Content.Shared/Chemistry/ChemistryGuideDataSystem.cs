using System.IO;
using System.Linq;
using Content.Shared.Chemistry.Reagent;
using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Chemistry;

/// <summary>
/// This handles the chemistry guidebook and caching it.
/// </summary>
public abstract class SharedChemistryGuideDataSystem : EntitySystem
{
    [Dependency] protected readonly INetManager Net = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    protected Dictionary<string, ReagentGuideEntry> _reagentGuideRegistry = new();

    public IReadOnlyDictionary<string, ReagentGuideEntry> ReagentGuideRegistry => _reagentGuideRegistry;

    /// <inheritdoc/>
    public override void Initialize()
    {
        _prototype.PrototypesReloaded += PrototypeReload;

        InitializeServerRegistry();
    }

    private void InitializeServerRegistry()
    {

    }

    private void PrototypeReload(PrototypesReloadedEventArgs obj)
    {
        if (Net.IsClient)
            return;

        if (!obj.ByType.TryGetValue(typeof(ReagentPrototype), out var reagents))
        {
            return;
        }

        var msg = new MsgUpdateReagentGuideRegistry()
        {
            Changeset = new ReagentGuideChangeset(new Dictionary<string, ReagentGuideEntry>(), new HashSet<string>())
        };
        foreach (var (id, proto) in reagents.Modified)
        {
            var reagentProto = (ReagentPrototype) proto;
            msg.Changeset.GuideEntries.Add(id, new ReagentGuideEntry(reagentProto, _prototype, EntityManager.EntitySysManager));
            _reagentGuideRegistry[id] = new ReagentGuideEntry(reagentProto, _prototype, EntityManager.EntitySysManager);
        }
        Net.ServerSendToAll(msg);
    }
}

public sealed class MsgUpdateReagentGuideRegistry : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.String;
    // This could break with prototype loads if unordered.
    public override NetDeliveryMethod DeliveryMethod { get; } = NetDeliveryMethod.ReliableOrdered;

    public ReagentGuideChangeset Changeset = default!;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        var length = buffer.ReadVariableInt32();
        using var stream = buffer.ReadAlignedMemory(length);
        serializer.DeserializeDirect(stream, out Changeset);
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        var stream = new MemoryStream();
        DebugTools.AssertNotNull(Changeset);
        serializer.SerializeDirect(stream, Changeset!);

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
