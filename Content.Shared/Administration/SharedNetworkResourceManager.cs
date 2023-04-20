using Robust.Shared.ContentPack;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Administration;

/// <summary>
///     Manager that allows resources to be added at runtime by admins.
///     They will be sent to all clients automatically.
/// </summary>
public abstract class SharedNetworkResourceManager : IDisposable
{
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] protected readonly IResourceManager ResourceManager = default!;

    public const double BytesToMegabytes = 0.000001d;

    /// <summary>
    ///     The prefix for any and all downloaded network resources.
    /// </summary>
    private static readonly ResourcePath Prefix = ResourcePath.Root / "Uploaded";

    protected readonly MemoryContentRoot ContentRoot = new();

    public virtual void Initialize()
    {
        _netManager.RegisterNetMessage<NetworkResourceUploadMessage>(ResourceUploadMsg);

        // Add our content root to the resource manager.
        ResourceManager.AddRoot(Prefix, ContentRoot);
    }

    protected abstract void ResourceUploadMsg(NetworkResourceUploadMessage msg);

    public void Dispose()
    {
        // This is called automatically when the IoCManager's dependency collection is cleared.
        // MemoryContentRoot uses a ReaderWriterLockSlim, which we need to dispose of.
        ContentRoot.Dispose();
    }

    // TODO REPLAYS
    // Figure out a way to just directly save NetMessage objects to replays. This just uses IRobustSerializer as a crutch.

    [Serializable, NetSerializable]
    public sealed class ReplayResourceUploadMsg
    {
        public byte[] Data = default!;
        public ResourcePath RelativePath = default!;
    }
}
