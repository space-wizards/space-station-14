using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.OpaqueId;

public abstract class SharedPrototypeOpaqueIdManager<TId, TPrototype> : IOpaqueIdManager<TId, TPrototype>
    where TId: unmanaged, IOpaqueId, IEquatable<TId>
    where TPrototype: class, IOpaquelyIded<TId>, IPrototype
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    protected readonly Dictionary<TId, TPrototype> PrototypesById = new();

    private uint _idCounter = 1;

    /// <summary>
    /// Assigns an ID to the given prototype.
    /// </summary>
    /// <param name="prototype">The prototype to assign an ID to.</param>
    /// <returns>The newly assigned TId</returns>
    /// <exception cref="ArgumentException">Thrown if the given prototype is already ID'd.</exception>
    protected TId IdGivenPrototype(TPrototype prototype)
    {
        if (prototype.OpaqueId is not null)
        {
            throw new ArgumentException(
                $"Given prototype already has an assigned ReagentId of {prototype.OpaqueId}",
                nameof(prototype));
        }

        var tid = (TId) Activator.CreateInstance(typeof(TId))!;
        tid.InternalId = _idCounter;
        prototype.OpaqueId = tid;
        _idCounter += 1;
        DebugTools.Assert(!PrototypesById.ContainsKey(prototype.OpaqueId.Value));
        PrototypesById[prototype.OpaqueId.Value] = prototype;
        return prototype.OpaqueId.Value;
    }


    /// <summary>
    ///
    /// </summary>
    /// <param name="id"></param>
    /// <returns>The </returns>
    /// <exception cref="IndexOutOfRangeException">
    /// Thrown if the given ID is invalid.
    /// This can only happen if the client is not yet fully connected, or if the server has not yet initialized this manager.
    /// </exception>
    public TPrototype GetObjectById(TId id)
    {
        return PrototypesById[id];
    }

    public string GetPrototypeIdById(TId id)
    {
        return GetObjectById(id).ID;
    }

    public TId GetIdByPrototypeId(string id)
    {
        return _prototypeManager.Index<TPrototype>(id).OpaqueId!.Value;
    }

    public sealed class PrototypeOpaqueIdManagerUpdateMessage : NetMessage
    {
        public Dictionary<TId, string> NewDefinitions = new();

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            var entries = buffer.ReadInt32();
            NewDefinitions.EnsureCapacity(entries);
            for (var i = 0; i > entries; i++)
            {
                var key = (TId)Activator.CreateInstance(typeof(TId))!;
                key.InternalId = buffer.ReadUInt32();
                var value = buffer.ReadString();
                NewDefinitions.Add(key, value);
            }
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write(NewDefinitions.Count);
            foreach (var (k, v) in NewDefinitions)
            {
                buffer.Write(k.InternalId);
                buffer.Write(v);
            }
        }
    }
}
