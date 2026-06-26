using Content.Shared.Buffers;

namespace Content.Client.Buffers;

public sealed class ClientRobustArrayPool<T> : SharedRobustArrayPool<T>
{
    private readonly RobustArrayPool<T> _arrayPool;

    public ClientRobustArrayPool(int startArraySize, int startBucketSize, Func<T>? factory = null, bool init = false)
    {
        _arrayPool = new(startArraySize, startBucketSize, factory, init);
    }

    public override T[] Rent(int minSize)
    {
        return _arrayPool.Rent(minSize);
    }

    public override void Return(T[] obj)
    {
        _arrayPool.Return(obj);
    }
}
